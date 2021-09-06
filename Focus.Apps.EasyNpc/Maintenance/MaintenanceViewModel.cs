using Focus.Apps.EasyNpc.Configuration;
using Focus.Apps.EasyNpc.Profiles;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;

namespace Focus.Apps.EasyNpc.Maintenance
{
    [AddINotifyPropertyChangedInterface]
    public class MaintenanceViewModel
    {
        public delegate MaintenanceViewModel Factory(Profile profile);

        public int AutosaveInvalidNpcCount { get; private set; }
        public int AutosaveRecordCount { get; private set; }
        public int AutoSaveRedundantRecordCount { get; private set; }
        [DependsOn("IsDeletingLogFiles")]
        public bool CanDeleteLogFiles => !IsDeletingLogFiles;
        [DependsOn("IsResettingNpcs")]
        public bool CanResetNpcs => !IsResettingNpcs;
        [DependsOn("IsTrimmingAutoSave")]
        public bool CanTrimAutoSave => !IsTrimmingAutoSave;
        public bool IsDeletingLogFiles { get; private set; }
        public bool IsResettingNpcs { get; private set; }
        public bool IsTrimmingAutoSave { get; private set; }
        public int LogFileCount { get; private set; }
        public decimal LogFileSizeMb { get; private set; }
        public bool OnlyResetInvalid { get; set; }

        private readonly IReadOnlySet<IRecordKey> npcKeys;
        private readonly IProfileEventLog profileEventLog;
        private readonly Profile profile;

        public MaintenanceViewModel(Profile profile, IProfileEventLog profileEventLog)
        {
            this.profile = profile;
            this.profileEventLog = profileEventLog;

            npcKeys = profile.Npcs.Select(x => new RecordKey(x)).ToHashSet(RecordKeyComparer.Default);
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
            IsResettingNpcs = true;
            try
            {
                var resetPredicate = GetResetPredicate(NpcProfileField.DefaultPlugin);
                // This is only going to work on configurations that are actually loaded, i.e. for NPCs that are present
                // in the current load order AND have at least one override. It seems somehow unintuitive that this
                // won't clean up all the garbage from previous runs, but on the other hand, that's how the autosave
                // system is actually supposed to work - NPCs that are no longer "valid" are simply ignored on this run
                // but will come back with their previous settings (or best available alternative) if restored.
                //
                // Resetting is distinct from trimming; if someone has made major changes to their load order and wants
                // to ensure that their profile/autosave is absolutely squeaky clean, they should trim, THEN reset.
                foreach (var npc in profile.Npcs)
                    if (resetPredicate(npc))
                        npc.ApplyPolicy(resetDefaultPlugin: true);
            }
            finally
            {
                IsResettingNpcs = false;
            }
        }

        public void ResetNpcFaces()
        {
            IsResettingNpcs = true;
            try
            {
                var resetPredicate = GetResetPredicate(NpcProfileField.FacePlugin);
                // Refer to caveats in ResetNpcDefaults.
                foreach (var npc in profile.Npcs)
                    if (resetPredicate(npc))
                        npc.ApplyPolicy(resetFacePlugin: true);
            }
            finally
            {
                IsResettingNpcs = false;
            }
        }

        public void TrimAutoSave()
        {
            IsTrimmingAutoSave = true;
            try
            {
                profileEventLog.Erase();
                foreach (var npc in profile.Npcs)
                    npc.WriteToEventLog();
                RefreshProfileStats();
            }
            finally
            {
                IsTrimmingAutoSave = false;
            }
        }

        private Predicate<Npc> GetResetPredicate(NpcProfileField field)
        {
            if (!OnlyResetInvalid)
                return npc => true;
            return field switch
            {
                NpcProfileField.DefaultPlugin => npc => !string.IsNullOrEmpty(npc.MissingDefaultPluginName),
                NpcProfileField.FacePlugin => npc => !string.IsNullOrEmpty(npc.MissingFacePluginName),
                _ => npc => true
            };
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
            AutosaveInvalidNpcCount = profileEvents.Where(x => !npcKeys.Contains(x)).Count();
            // To detect redundant events, it's more efficient to check what ISN'T redundant and then subtract.
            var profileEventGroups = profileEvents
                .GroupBy(x => Tuple.Create(x.BasePluginName, x.LocalFormIdHex, x.Field))
                .ToList();
            AutoSaveRedundantRecordCount = AutosaveRecordCount - profileEventGroups.Count;
        }
    }
}