﻿using Focus.Apps.EasyNpc.Configuration;
using Focus.Apps.EasyNpc.Profile;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;

namespace Focus.Apps.EasyNpc.Maintenance
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

        private readonly IReadOnlySet<string> loadedPlugins;
        private readonly IReadOnlyList<NpcConfiguration<TKey>> npcConfigs;
        private readonly IReadOnlySet<Tuple<string, string>> npcKeys;
        private readonly ProfileEventLog profileEventLog;

        public MaintenanceViewModel(
            IEnumerable<NpcConfiguration<TKey>> npcConfigs, ProfileEventLog profileEventLog,
            IEnumerable<string> loadedPlugins)
        {
            this.npcConfigs = npcConfigs.ToList().AsReadOnly();
            npcKeys = npcConfigs.Select(x => Tuple.Create(x.BasePluginName, x.LocalFormIdHex)).ToHashSet();
            this.profileEventLog = profileEventLog;
            this.loadedPlugins = loadedPlugins.ToHashSet(StringComparer.OrdinalIgnoreCase);
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
                foreach (var npcConfig in npcConfigs)
                    if (resetPredicate(npcConfig))
                        npcConfig.Reset(defaults: true, faces: false);
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
                foreach (var npcConfig in npcConfigs)
                    if (resetPredicate(npcConfig))
                        npcConfig.Reset(defaults: false, faces: true);
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

        private Predicate<NpcConfiguration<TKey>> GetResetPredicate(NpcProfileField field)
        {
            if (!OnlyResetInvalid)
                return npc => true;
            var filteredNpcs = profileEventLog
                .MostRecentByNpc()
                .Where(e => e.Field == field)
                .WithMissingPlugins(loadedPlugins)
                .Select(e => Tuple.Create(e.BasePluginName, e.LocalFormIdHex))
                .ToHashSet();
            return npc => filteredNpcs.Contains(Tuple.Create(npc.BasePluginName, npc.LocalFormIdHex));
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