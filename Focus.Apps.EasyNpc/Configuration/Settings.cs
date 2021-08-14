﻿using Focus.Apps.EasyNpc.Build;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reactive.Subjects;

namespace Focus.Apps.EasyNpc.Configuration
{
    public class Settings : IAppSettings, IObservableAppSettings
    {
        // This needs to be first (contrary to default sorting rules) so that it can be used in the Default
        // construction.
        private static readonly JsonSerializer Serializer = new()
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy(),
            },
            Formatting = Formatting.Indented,
        };

        public static readonly Settings Default = new(ProgramData.SettingsPath);

        
        [JsonIgnore] // Report path is a command-line setting used for interop; we don't save it.
        public string BuildReportPath { get; set; } = Path.Combine(ProgramData.DirectoryPath, "BuildReport.json");

        public IReadOnlyList<BuildWarningSuppression> BuildWarningWhitelist
        {
            get => buildWarningWhitelist.Value;
            set => buildWarningWhitelist.OnNext(value);
        }

        public string ModRootDirectory
        {
            get => modRootDirectory.Value;
            set => modRootDirectory.OnNext(value);
        }

        public IReadOnlyList<MugshotRedirect> MugshotRedirects
        {
            get => mugshotRedirects.Value;
            set => mugshotRedirects.OnNext(value);
        }

        public string MugshotsDirectory
        {
            get => mugshotsDirectory.Value;
            set => mugshotsDirectory.OnNext(value);
        }

        public string StaticAssetsPath => ProgramData.AssetsPath;

        [JsonIgnore]
        public IObservable<IReadOnlyList<BuildWarningSuppression>> BuildWarningWhitelistObservable =>
            buildWarningWhitelist;
        [JsonIgnore]
        public IObservable<string> ModRootDirectoryObservable => modRootDirectory;
        [JsonIgnore]
        public IObservable<IReadOnlyList<MugshotRedirect>> MugshotRedirectsObservable => mugshotRedirects;
        [JsonIgnore]
        public IObservable<string> MugshotsDirectoryObservable => mugshotsDirectory;

        IEnumerable<BuildWarningSuppression> IAppSettings.BuildWarningWhitelist => BuildWarningWhitelist;
        IEnumerable<MugshotRedirect> IAppSettings.MugshotRedirects => MugshotRedirects;

        private readonly BehaviorSubject<IReadOnlyList<BuildWarningSuppression>> buildWarningWhitelist =
            new(new List<BuildWarningSuppression>());
        private readonly BehaviorSubject<string> modRootDirectory = new(string.Empty);
        private readonly BehaviorSubject<IReadOnlyList<MugshotRedirect>> mugshotRedirects =
            new(GetDefaultMugshotRedirects());
        private readonly BehaviorSubject<string> mugshotsDirectory = new(string.Empty);
        private readonly string path;

        public Settings(string path)
        {
            this.path = path;
            Load();
        }

        public bool Load()
        {
            if (!File.Exists(path))
                return false;
            using var fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var streamReader = new StreamReader(fs);
            using var jsonReader = new JsonTextReader(streamReader);
            // Using Serializer.Populate(this) would require us to clear various lists to avoid duplicates, since
            // JSON.NET doesn't clear them prior to populating. However, this obscures the distinction between settings
            // with default values and missing settings. It's more tedious, but better to just deserialize an entirely
            // new settings object and copy the fields directly.
            var settings = Serializer.Deserialize<Settings>(jsonReader);
            CopyFrom(settings);
            return true;
        }

        public void Save()
        {
            using var fs = File.Create(path);
            using var streamWriter = new StreamWriter(fs);
            using var jsonWriter = new JsonTextWriter(streamWriter);
            Serializer.Serialize(jsonWriter, this);
        }

        private void CopyFrom(Settings other)
        {
            // This doesn't make deep copies, as it's assumed to only be working with temporary Settings instances, i.e.
            // from the Load() method. If this method is made public, the behavior needs to change.
            BuildWarningWhitelist = other.BuildWarningWhitelist;
            ModRootDirectory = other.ModRootDirectory;
            MugshotRedirects = other.MugshotRedirects;
            MugshotsDirectory = other.MugshotsDirectory;
        }

        private static List<MugshotRedirect> GetDefaultMugshotRedirects()
        {
            return new()
            {
                // This is a known issue as mugshots for PoS were released under the "wrong" name.
                // Rather than having to upload an entirely new pack just to fix a directory name, this can be added as a
                // default redirect and automatically appear for anyone who upgrades from the old version.
                new MugshotRedirect
                {
                    ModName = "Pride of Skyrim - AIO Male High Poly Head Overhaul",
                    Mugshots = "Pride of Skyrim - AIO",
                },
                new MugshotRedirect
                {
                    ModName = "Courageous Women - High Poly Head Female NPC Overhaul",
                    Mugshots = "Courageous Women of Skyrim AIO",
                },
                new MugshotRedirect
                {
                    ModName = "Fresh Faces",
                    Mugshots = "Fresh Faces - SSE",
                },
                new MugshotRedirect
                {
                    ModName = "Pandorable's Frea",
                    Mugshots = "Pandorable's Frea and Frida",
                },
                new MugshotRedirect
                {
                    ModName = "Pandorable's Lethal Ladies - Jenassa Karliah",
                    Mugshots = "Pandorable's Lethal Ladies",
                },
                new MugshotRedirect
                {
                    ModName = "Pandorable's Shield-Sisters - Aela Ria Njada",
                    Mugshots = "Pandorable's Shield Sisters",
                },
                new MugshotRedirect
                {
                    ModName = "Pandorable's Warrior Women - Mjoll Uthgerd",
                    Mugshots = "Pandorable's Warrior Women",
                },
            };
        }
    }

    public class BuildWarningSuppression
    {
        public string PluginName { get; set; }
        [JsonProperty(ItemConverterType = typeof(StringEnumConverter))]
        public List<BuildWarningId> IgnoredWarnings { get; set; } = new();

        public BuildWarningSuppression()
        {
        }

        public BuildWarningSuppression(string pluginName, IEnumerable<BuildWarningId> ignoredWarnings)
        {
            PluginName = pluginName;
            IgnoredWarnings = ignoredWarnings.ToList();
        }
    }

    public class MugshotRedirect
    {
        public string ModName { get; set; } 
        public string Mugshots { get; set; }

        public MugshotRedirect() { }

        public MugshotRedirect(string modName, string mugshots)
        {
            ModName = modName;
            Mugshots = mugshots;
        }
    }
}
