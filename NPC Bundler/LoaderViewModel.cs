﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XeLib;
using XeLib.API;

namespace NPC_Bundler
{
    public class LoaderViewModel : INotifyPropertyChanged
    {
        public event Action Loaded;
        public event PropertyChangedEventHandler PropertyChanged;

        public bool CanLoad { get; private set; }
        public bool IsLoading { get; private set; } = true;
        public bool IsLogVisible { get; private set; }
        public bool IsPluginListVisible { get; private set; }
        public bool IsSpinnerVisible { get; private set; }
        public IReadOnlyList<string> LoadedPluginNames { get; private set; }
        public LogViewModel Log { get; init; }
        public IReadOnlyList<Npc> Npcs { get; private set; }
        public IReadOnlyList<PluginSetting> Plugins { get; private set; }
        public string Status { get; private set; }

        public LoaderViewModel(LogViewModel log)
        {
            Log = log;

            Status = "Starting up...";
            IsSpinnerVisible = true;
            CanLoad = false;

            Log.Resume();
            Meta.Initialize();
            Setup.SetGameMode(Setup.GameMode.SSE);
            Plugins = Setup.GetActivePlugins()
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select((fileName, i) => new PluginSetting(fileName, i + 1))
                .ToList()
                .AsReadOnly();
            Status = "Confirm plugin selection and load order.";
            IsSpinnerVisible = false;
            IsPluginListVisible = true;
            CanLoad = true;
        }

        public async void ConfirmPlugins()
        {
            CanLoad = false;
            Status = "Loading selected plugins...";
            IsSpinnerVisible = true;
            IsPluginListVisible = false;
            IsLogVisible = true;

            var loadOrder = string.Join('\n', Plugins.Where(x => x.ShouldLoad).Select(x => x.FileName));
            Setup.LoadPlugins(loadOrder, true);
            await WaitForLoad().ConfigureAwait(true);

            LoadedPluginNames = Setup.GetLoadedFileNames();
            Status = "Done loading plugins. Building NPC index...";
            Npcs = new List<Npc>(await Task.Run(GetNpcs).ConfigureAwait(true));
            Log.Append("All NPCs loaded.");

            Log.Pause();
            Status = "All done.";
            IsSpinnerVisible = false;
            IsLoading = false;
            Loaded?.Invoke();
        }

        private IEnumerable<NpcInfo> GetNpcs()
        {
            var npcs = new Dictionary<uint, NpcInfo>();

            using var g = new HandleGroup();
            foreach (var fileName in LoadedPluginNames)
            {
                Log.Append($"Reading NPC records from {fileName}...");
                var file = g.AddHandle(Files.FileByName(fileName));
                var npcRecords = g.AddHandles(Records.GetRecords(file, "NPC_", true));
                foreach (var npcRecord in npcRecords)
                {
                    var formId = Records.GetFormId(npcRecord, false, false);
                    if (Records.IsOverride(npcRecord))
                    {
                        var npc = npcs[formId];
                        var faceOverrides = Npc.GetFaceOverrides(npcRecord);
                        npc.Overrides.Add(new NpcOverride(fileName, faceOverrides));
                    } else
                    {
                        npcs.Add(formId, new NpcInfo
                        {
                            BasePluginName = fileName,
                            EditorId = RecordValues.GetEditorId(npcRecord),
                            FormId = Records.GetFormId(npcRecord, false, false),
                            LocalFormIdHex = Records.GetHexFormId(npcRecord, false, true),
                            // Checking for FULL isn't totally necessary, but avoids spamming log warnings.
                            Name = Elements.HasElement(npcRecord, "FULL") ?
                                ElementValues.GetValue(npcRecord, "FULL") : "",
                        });
                    }
                }
            }

            return npcs.Values.OrderBy(x => x.FormId);
        }

        private async Task WaitForLoad()
        {
            Setup.LoaderState loaderStatus;
            do
            {
                loaderStatus = Setup.GetLoaderStatus();
                if (loaderStatus != Setup.LoaderState.IsActive)
                    break;
                await Task.Delay(50);
            } while (true);
        }
    }

    public class PluginSetting : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public string FileName { get; init; }
        public int Index { get; init; }
        public bool ShouldLoad { get; set; }

        public PluginSetting(string fileName, int index)
        {
            FileName = fileName;
            Index = index;
            ShouldLoad = true;
        }
    }

    class NpcInfo : Npc
    {
        public string BasePluginName { get; set; }
        public string EditorId { get; set; }
        public uint FormId { get; set; }
        public string LocalFormIdHex { get; set; }
        public string Name { get; set; }
        public List<NpcOverride> Overrides { get; set; } = new List<NpcOverride>();

        IReadOnlyList<NpcOverride> Npc.Overrides => Overrides.AsReadOnly();
    }
}
