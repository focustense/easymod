﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.RegularExpressions;
using Focus.Apps.EasyNpc.Configuration;
using Focus.Apps.EasyNpc.GameData.Files;
using Focus.Apps.EasyNpc.Profiles;
using Focus.Apps.EasyNpc.Reports;
using PropertyChanged;

namespace Focus.Apps.EasyNpc.Build.Preview
{
    [AddINotifyPropertyChangedInterface]
    public class OutputViewModel
    {
        public delegate OutputViewModel Factory(Profile profile);

        private static readonly char[] InvalidPathChars = Path.GetInvalidFileNameChars()
            .Where(c => c != Path.DirectorySeparatorChar && c != Path.AltDirectorySeparatorChar)
            .Append('?') // Why isn't it included?
            .ToArray();
        private static readonly Regex InvalidPathRegex =
            new("[" + Regex.Escape(new string(InvalidPathChars)) + "]", RegexOptions.Compiled);

        public IObservable<BuildSettings> BuildSettings { get; private init; }

        public bool EnableDewiggify
        {
            get => enableDewiggify.Value;
            set => enableDewiggify.OnNext(value);
        }

        public decimal AssetSizeCompressedGb { get; private set; }
        public decimal AssetSizeUncompressedGb { get; private set; }
        public bool IsExistingMod { get; private set; }
        public bool IsValidDirectory { get; private set; }

        public string ModName
        {
            get => modName.Value;
            set => modName.OnNext(value);
        }

        public string PluginName => FileStructure.MergeFileName;

        [DependsOn(
            nameof(IsExistingMod), nameof(IsValidDirectory), nameof(AssetSizeCompressedGb),
            nameof(AssetSizeUncompressedGb))]
        public IEnumerable<SummaryItem> SummaryItems => new SummaryItem[]
        {
            IsValidDirectory ?
                new(SummaryItemCategory.StatusOk, "Output path is valid") :
                new(SummaryItemCategory.StatusError, "Invalid output path"),
            IsExistingMod ?
                new(SummaryItemCategory.StatusError, "Output already exists") :
                new(SummaryItemCategory.StatusOk, "Output directory is empty"),
            new(SummaryItemCategory.StatusInfo, "GB assets", AssetSizeUncompressedGb),
            new(SummaryItemCategory.StatusInfo, "GB packed (est.)", AssetSizeCompressedGb),
        };

        private readonly BehaviorSubject<bool> enableDewiggify = new(true);
        private readonly IFileSystem fs;
        private readonly BehaviorSubject<string> modName = new($"NPC Merge {DateTime.Now:yyyy-MM-dd}");
        private readonly Profile profile;

        public OutputViewModel(IObservableModSettings modSettings, IFileSystem fs, Profile profile)
        {
            this.fs = fs;
            this.profile = profile;
            BuildSettings = Observable
                .CombineLatest(
                    modSettings.RootDirectoryObservable, modName, enableDewiggify, GetBuildSettings);
            BuildSettings.Subscribe(settings =>
            {
                IsExistingMod = IsOutputDirectoryNonEmpty(settings);
                IsValidDirectory = IsOutputDirectoryValid(settings);
            });
        }

        private BuildSettings GetBuildSettings(
            string modRootDirectory, string outputModName, bool enableDewiggify)
        {
            outputModName = outputModName ?? "";
            var outputDirectory = Path.Combine(modRootDirectory, outputModName);
            return new BuildSettings(profile, outputDirectory, outputModName)
            {
                EnableDewiggify = enableDewiggify,
            };
        }

        private bool IsOutputDirectoryNonEmpty(BuildSettings settings)
        {
            return fs.Directory.Exists(settings.OutputDirectory) &&
                fs.Directory.EnumerateFiles(settings.OutputDirectory).Any();
        }

        private bool IsOutputDirectoryValid(BuildSettings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.OutputModName))
                return false;
            try
            {
                if (!fs.Path.IsPathRooted(settings.OutputDirectory))
                    return false;
                var root = fs.Path.GetPathRoot(settings.OutputDirectory);
                return !InvalidPathRegex.IsMatch(settings.OutputDirectory[root.Length..]);
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
