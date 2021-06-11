using PropertyChanged;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;

namespace NPC_Bundler
{
    public class MaintenanceViewModel<TKey> : INotifyPropertyChanged
        where TKey : struct
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public int AutosaveInvalidNpcCount { get; private set; }
        public int AutosaveRecordCount { get; private set; }
        public int AutoSaveRedundantRecordCount { get; private set; }
        [DependsOn("IsDeletingLogFiles")]
        public bool CanDeleteLogFiles => !IsDeletingLogFiles;
        [DependsOn("IsResettingNpcDefaults")]
        public bool CanResetNpcDefaults => !IsResettingNpcDefaults;
        [DependsOn("IsTrimmingAutoSave")]
        public bool CanTrimAutoSave => !IsTrimmingAutoSave;
        public bool IsDeletingLogFiles { get; private set; }
        public bool IsResettingNpcDefaults { get; private set; }
        public bool IsTrimmingAutoSave { get; private set; }
        public int LogFileCount { get; private set; }
        public decimal LogFileSizeMb { get; private set; }

        private readonly IReadOnlyList<NpcConfiguration<TKey>> npcConfigs;
        private readonly IReadOnlySet<Tuple<string, string>> npcKeys;
        private readonly ProfileEventLog profileEventLog;

        public MaintenanceViewModel(IEnumerable<NpcConfiguration<TKey>> npcConfigs, ProfileEventLog profileEventLog)
        {
            this.npcConfigs = npcConfigs.ToList().AsReadOnly();
            npcKeys = npcConfigs.Select(x => Tuple.Create(x.BasePluginName, x.LocalFormIdHex)).ToHashSet();
            this.profileEventLog = profileEventLog;
        }

        public void DeleteOldLogFiles()
        {
            IsDeletingLogFiles = true;
            try
            {
                var logFileNames = Directory.GetFiles(ProgramData.DirectoryPath, "Log_*.txt")
                    .Where(f => f != ProgramData.LogFileName)
                    .ToList();
                foreach (var logFileName in logFileNames)
                    File.Delete(logFileName);
                RefreshLogStats();
            }
            finally
            {
                IsDeletingLogFiles = false;
            }
        }

        public void Refresh()
        {
            RefreshLogStats();
            RefreshProfileStats();
        }

        public void ResetNpcDefaults()
        {
            IsResettingNpcDefaults = true;
            try
            {
                // This is only going to work on configurations that are actually loaded, i.e. for NPCs that are present
                // in the current load order AND have at least one override. It seems somehow unintuitive that this
                // won't clean up all the garbage from previous runs, but on the other hand, that's how the autosave
                // system is actually supposed to work - NPCs that are no longer "valid" are simply ignored on this run
                // but will come back with their previous settings (or best available alternative) if restored.
                //
                // Resetting is distinct from trimming; if someone has made major changes to their load order and wants
                // to ensure that their profile/autosave is absolutely squeaky clean, they should trim, THEN reset.
                foreach (var npcConfig in npcConfigs)
                    npcConfig.Reset();
            }
            finally
            {
                IsResettingNpcDefaults = false;
            }
        }

        public void TrimAutoSave()
        {
            IsTrimmingAutoSave = true;
            try
            {
                profileEventLog.Erase();
                var fieldTypes = Enum.GetValues<NpcProfileField>();
                foreach (var npcConfig in npcConfigs)
                    npcConfig.EmitProfileEvents(fieldTypes);
                RefreshProfileStats();
            }
            finally
            {
                IsTrimmingAutoSave = false;
            }
        }

        private void RefreshLogStats()
        {
            var logFileNames = Directory.GetFiles(ProgramData.DirectoryPath, "Log_*.txt")
                .Where(f => f != ProgramData.LogFileName)
                .ToList();
            LogFileCount = logFileNames.Count;
            LogFileSizeMb = (decimal)Math.Round(
                logFileNames.Select(f => new FileInfo(f).Length / 1024f / 1024f).Sum(), 1);
        }

        private void RefreshProfileStats()
        {
            var profileEvents = ProfileEventLog.ReadEventsFromFile(ProgramData.ProfileLogFileName).ToList();
            AutosaveRecordCount = profileEvents.Count;
            AutosaveInvalidNpcCount = profileEvents
                .Where(x => !npcKeys.Contains(Tuple.Create(x.BasePluginName, x.LocalFormIdHex)))
                .Count();
            // To detect redundant events, it's more efficient to check what ISN'T redundant and then subtract.
            var profileEventGroups = profileEvents
                .GroupBy(x => Tuple.Create(x.BasePluginName, x.LocalFormIdHex, x.Field))
                .ToList();
            AutoSaveRedundantRecordCount = AutosaveRecordCount - profileEventGroups.Count;
        }
    }
}