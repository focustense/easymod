using Focus.Apps.EasyNpc.Build;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Focus.Apps.EasyNpc.Configuration
{
    public class Settings : IAppSettings, IMutableAppSettings, IObservableAppSettings
    {
        public static readonly Settings Default = new(ProgramData.SettingsPath);

        public string BuildReportPath { get; set; } = Path.Combine(ProgramData.DirectoryPath, "BuildReport.json");

        public IReadOnlyList<BuildWarningSuppression> BuildWarningWhitelist
        {
            get => buildWarningWhitelist.Value;
            set => buildWarningWhitelist.OnNext(value);
        }

        public string DefaultModRootDirectory
        {
            get => defaultModRootDirectory.Value;
            set => defaultModRootDirectory.OnNext(value);
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

        public bool UseModManagerForModDirectory
        {
            get => useModManagerForModDirectory.Value;
            set => useModManagerForModDirectory.OnNext(value);
        }

        public IObservable<IReadOnlyList<BuildWarningSuppression>> BuildWarningWhitelistObservable =>
            buildWarningWhitelist;
        public IObservable<string> DefaultModRootDirectoryObservable => defaultModRootDirectory;
        public IObservable<IReadOnlyList<MugshotRedirect>> MugshotRedirectsObservable => mugshotRedirects;
        public IObservable<string> MugshotsDirectoryObservable => mugshotsDirectory;
        public IObservable<bool> UseModManagerForModDirectoryObservable => useModManagerForModDirectory;

        IEnumerable<BuildWarningSuppression> IAppSettings.BuildWarningWhitelist => BuildWarningWhitelist;
        IEnumerable<MugshotRedirect> IAppSettings.MugshotRedirects => MugshotRedirects;

        private readonly BehaviorSubject<IReadOnlyList<BuildWarningSuppression>> buildWarningWhitelist =
            new(new List<BuildWarningSuppression>());
        private readonly BehaviorSubject<string> defaultModRootDirectory = new(string.Empty);
        private readonly BehaviorSubject<IReadOnlyList<MugshotRedirect>> mugshotRedirects =
            new(GetDefaultMugshotRedirects());
        private readonly BehaviorSubject<string> mugshotsDirectory = new(string.Empty);
        private readonly string path;
        private readonly BehaviorSubject<bool> useModManagerForModDirectory = new(true);

        public Settings(string path)
        {
            this.path = path;
            Load();
        }

        public bool Load()
        {
            var data = File.Exists(path) ? SettingsData.LoadFromFile(path) : null;
            if (data is null)
                return false;
            if (data.BuildWarningWhitelist is not null)
                foreach (var entry in data.BuildWarningWhitelist)
                    entry.IgnoredWarnings.RemoveAll(x => x == BuildWarningId.Unknown);
            CopyFrom(data);
            return true;
        }

        public void Save()
        {
            var data = new SettingsData
            {
                BuildWarningWhitelist = BuildWarningWhitelist,
                ModRootDirectory = DefaultModRootDirectory,
                MugshotRedirects = MugshotRedirects,
                MugshotsDirectory = MugshotsDirectory,
                UseModManagerForModDirectory = UseModManagerForModDirectory,
            };
            data.SaveToFile(path);
        }

        private void CopyFrom(SettingsData data)
        {
            if (data.BuildWarningWhitelist is not null)
                BuildWarningWhitelist = data.BuildWarningWhitelist;
            if (!string.IsNullOrEmpty(data.ModRootDirectory))
                DefaultModRootDirectory = data.ModRootDirectory;
            if (data.MugshotRedirects is not null)
                MugshotRedirects = data.MugshotRedirects;
            if (!string.IsNullOrEmpty(data.MugshotsDirectory))
                MugshotsDirectory = data.MugshotsDirectory;
            if (data.UseModManagerForModDirectory.HasValue)
                UseModManagerForModDirectory = data.UseModManagerForModDirectory.Value;
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
        public string PluginName { get; set; } = string.Empty;
        [JsonProperty(
            ItemConverterType = typeof(SafeStringEnumConverter),
            ItemConverterParameters = new object[] { BuildWarningId.Unknown })]
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
        public string ModName { get; set; } = string.Empty;
        public string Mugshots { get; set; } = string.Empty;

        public MugshotRedirect() { }

        public MugshotRedirect(string modName, string mugshots)
        {
            ModName = modName;
            Mugshots = mugshots;
        }
    }

    public class SettingsData
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

        public static SettingsData? LoadFromFile(string path)
        {
            using var fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var streamReader = new StreamReader(fs);
            using var jsonReader = new JsonTextReader(streamReader);
            // Using Serializer.Populate(this) would require us to clear various lists to avoid duplicates, since
            // JSON.NET doesn't clear them prior to populating. However, this obscures the distinction between settings
            // with default values and missing settings. It's more tedious, but better to just deserialize an entirely
            // new settings object and copy the fields directly.
            return Serializer.Deserialize<SettingsData>(jsonReader);
        }

        public void SaveToFile(string path)
        {
            using var fs = File.Create(path);
            using var streamWriter = new StreamWriter(fs);
            using var jsonWriter = new JsonTextWriter(streamWriter);
            Serializer.Serialize(jsonWriter, this);
        }

        public IReadOnlyList<BuildWarningSuppression>? BuildWarningWhitelist { get; set; }
        public string? ModRootDirectory { get; set; }
        public IReadOnlyList<MugshotRedirect>? MugshotRedirects { get; set; }
        public string? MugshotsDirectory { get; set; }
        public bool? UseModManagerForModDirectory { get; set; }
    }
}
