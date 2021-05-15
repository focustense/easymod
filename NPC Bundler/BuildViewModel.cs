using PropertyChanged;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XeLib.API;

namespace NPC_Bundler
{
    public class BuildViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        [DependsOn("Problems")]
        public bool HasProblems => Problems?.Count > 0;
        public bool IsProblemCheckerEnabled => !IsProblemCheckingInProgress;
        public bool IsProblemCheckerVisible { get; set; } = true;
        public bool IsProblemCheckingCompleted { get; set; }
        public bool IsProblemCheckingInProgress { get; set; }
        public IReadOnlyList<NpcConfiguration> Npcs { get; init; }
        public IReadOnlyList<BuildWarning> Problems { get; private set; }

        private readonly IReadOnlyList<string> loadOrder;

        public BuildViewModel(IEnumerable<NpcConfiguration> npcs, IReadOnlyList<string> loadOrder)
        {
            Npcs = npcs.ToList().AsReadOnly();
            this.loadOrder = loadOrder;
        }

        public async void CheckForProblems()
        {
            IsProblemCheckerVisible = false;
            IsProblemCheckingCompleted = false;
            IsProblemCheckingInProgress = true;
            var warnings = new List<BuildWarning>();
            await Task.Run(() =>
            {
                warnings.AddRange(CheckModSettings());
                warnings.AddRange(CheckForOverriddenArchives());
            });
            Problems = warnings.AsReadOnly();
            IsProblemCheckingInProgress = false;
            IsProblemCheckingCompleted = true;
        }

        public void DismissProblems()
        {
            IsProblemCheckingCompleted = false;
        }

        private IEnumerable<BuildWarning> CheckForOverriddenArchives()
        {
            // It is not - necessarily - a major problem for the game itself if multiple mods provide the same BSA.
            // The game, and this program, will simply use whichever version is actually loaded, i.e. from the last mod
            // in the list. However, there's no obvious way to tell *which* mod is currently providing that BSA.
            // This means that the user might select a mod for some NPC, and we might believe we are pulling facegen
            // data from that mod's archive, but in fact we are pulling it from a different version of the archive
            // provided by some other mod, which might be fine, or might be totally broken.
            // It's also extremely rare, with the only known instance (at the time of writing) being a patch to the
            // Sofia follower mod that removes a conflicting script, i.e. doesn't affect facegen data at all.
            // So it may be an obscure theoretical problem that never comes up in practice, but if we do see it, then
            // it at least merits a warning, which the user can ignore if it's on purpose.
            var modPluginMap = ModPluginMap.ForDirectory(BundlerSettings.Default.ModRootDirectory);
            return Resources.GetLoadedContainers().AsParallel()
                .Select(path => Path.GetFileName(path))
                .Select(f => new {
                    Name = f,
                    ProvidingMods = modPluginMap.GetModsForArchive(f).ToList()
                })
                .Where(x => x.ProvidingMods.Count > 1)
                .Select(x => new BuildWarning(
                    $"Archive '{x.Name}' is provided by multiple mods: [{string.Join(", ", x.ProvidingMods)}]."));
        }

        private IEnumerable<BuildWarning> CheckModSettings()
        {
            var modRootDirectory = BundlerSettings.Default.ModRootDirectory;
            if (string.IsNullOrWhiteSpace(modRootDirectory))
            {
                yield return new BuildWarning(WarningMessages.ModDirectoryNotSpecified());
                yield break;
            }
            if (!Directory.Exists(modRootDirectory))
            {
                yield return new BuildWarning(WarningMessages.ModDirectoryNotFound(modRootDirectory));
                yield break;
            }
        }

        static class WarningMessages
        {
            public static string ModDirectoryNotFound(string directoryName)
            {
                return $"Mod directory {directoryName} doesn't exist. {ModRootJustification}";
            }

            public static string ModDirectoryNotSpecified() {
                return "No mod directory specified in settings. " + ModRootJustification;
            }

            private static readonly string ModRootJustification =
                "Without direct access to your mod structure, this program can only generate a merged plugin, which " +
                "will probably break NPC appearances unless you are manually organizing the facegen data.";
        }
    }

    public enum BuildWarningId
    {
        Unspecified = 0,
    }

    public class BuildWarning
    {
        // Used for help links, if provided.
        public BuildWarningId Id { get; init; } = BuildWarningId.Unspecified;
        public string Message { get; init; }

        public BuildWarning() { }

        public BuildWarning(string message) : this()
        {
            Message = message;
        }

        public BuildWarning(BuildWarningId id, string message) : this(message)
        {
            Id = id;
        }
    }
}