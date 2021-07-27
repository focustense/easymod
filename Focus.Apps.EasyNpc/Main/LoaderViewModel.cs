using Focus.Apps.EasyNpc.Debug;
using Focus.Apps.EasyNpc.GameData.Files;
using Focus.Apps.EasyNpc.GameData.Records;
using Focus.Environment;
using PropertyChanged;
using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace Focus.Apps.EasyNpc.Main
{
    public class LoaderViewModel<TKey> : INotifyPropertyChanged
        where TKey : struct
    {
        public event Action Loaded;
        public event PropertyChangedEventHandler PropertyChanged;

        public bool CanLoad { get; private set; }
        public IReadOnlyList<Hair<TKey>> Hairs { get; private set; }
        public bool HasEnabledUnloadablePlugins { get; private set; }
        public bool IsLoading { get; private set; } = true;
        public bool IsLogVisible { get; private set; }
        public bool IsPluginListVisible { get; private set; }
        public bool IsSpinnerVisible { get; private set; }
        [DependsOn("HasEnabledUnloadablePlugins", "IsPluginListVisible")]
        public bool IsUnloadablePluginWarningVisible => HasEnabledUnloadablePlugins && IsPluginListVisible;
        public IReadOnlyList<string> LoadedMasterNames { get; private set; }
        public IReadOnlyList<string> LoadedPluginNames { get; private set; }
        public IReadOnlyLoadOrderGraph Graph => graph;
        public LogViewModel Log { get; private init; }
        public IModPluginMapFactory ModPluginMapFactory => editor.ModPluginMapFactory;
        public IReadOnlyList<INpc<TKey>> Npcs { get; private set; }
        public IReadOnlyList<PluginSetting> Plugins { get; private set; }
        public string Status { get; private set; }

        private readonly IGameDataEditor<TKey> editor;
        private readonly ILogger logger;
        private readonly LoadOrderGraph graph;

        public LoaderViewModel(IGameDataEditor<TKey> editor, LogViewModel logViewModel, ILogger logger)
        {
            this.editor = editor;
            Log = logViewModel;
            this.logger = logger;

            Status = "Starting up...";
            IsSpinnerVisible = true;
            CanLoad = false;

            var sourcePlugins = editor.GetAvailablePlugins().ToList();
            var blacklist = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { FileStructure.MergeFileName };
            graph = new(sourcePlugins, blacklist);
            Plugins = editor.GetAvailablePlugins()
                .Select((x, i) => new PluginSetting(x.FileName, i + 1, x.IsReadable, x.IsEnabled))
                .ToList()
                .AsReadOnly();
            UpdatePluginStates();
            foreach (var plugin in Plugins)
                plugin.Toggled += Plugin_Toggled;
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

            var loadOrder = Plugins.Where(x => x.CanLoad && x.ShouldLoad).Select(x => x.FileName);
            await editor.Load(loadOrder).ConfigureAwait(true);

            LoadedPluginNames = editor.GetLoadedPlugins().ToList().AsReadOnly();
            LoadedMasterNames = LoadedPluginNames
                .Where(pluginName => editor.IsMaster(pluginName))
                .ToList()
                .AsReadOnly();
            Status = "Done loading plugins. Collecting head part info...";
            Hairs = LoadedPluginNames
                .SelectMany(pluginName => editor.ReadHairRecords(pluginName))
                .Where(x => !string.IsNullOrEmpty(x.ModelFileName))
                .ToList()
                .AsReadOnly();
            Status = "Done loading head parts. Building NPC index...";
            var loadedNpcs = await Task.Run(GetNpcs).ConfigureAwait(true);
            Npcs = new List<INpc<TKey>>(loadedNpcs);
            logger.Information("All NPCs loaded.");

            Status = "Preparing game profile...";
            IsSpinnerVisible = false;
            IsLoading = false;
            Loaded?.Invoke();
        }

        public void TogglePlugins(IEnumerable<PluginSetting> plugins)
        {
            var materializedPlugins = plugins.ToList();
            if (materializedPlugins.Count == 0)
                return;
            var shouldLoad = materializedPlugins.Any(x => !x.ShouldLoad);
            foreach (var plugin in materializedPlugins)
                if (plugin.CanLoad) // Row shouldn't be editable otherwise
                    plugin.ShouldLoad = shouldLoad;
        }

        private IEnumerable<INpc<TKey>> GetNpcs()
        {
            var npcs = new Dictionary<TKey, IMutableNpc<TKey>>();
            foreach (var pluginName in LoadedPluginNames)
            {
                logger.Information($"Reading NPC records from {pluginName}...");
                editor.ReadNpcRecords(pluginName, npcs);
            }

            var loadOrderIndices = npcs.Values
                .Select(x => x.BasePluginName)
                .Distinct()
                .Select(pluginName => new
                {
                    PluginName = pluginName,
                    LoadOrderIndex = editor.GetLoadOrderIndex(pluginName)
                })
                .ToDictionary(x => x.PluginName, x => x.LoadOrderIndex);
            return npcs.Values
                .OrderBy(x => loadOrderIndices[x.BasePluginName])
                .ThenBy(x => Convert.ToInt32(x.LocalFormIdHex, 16));
        }

        private void Plugin_Toggled(object sender, EventArgs e)
        {
            var pluginSetting = (PluginSetting)sender;
            graph.SetEnabled(pluginSetting.FileName, pluginSetting.ShouldLoad);
            UpdatePluginStates();
        }

        private void UpdatePluginStates()
        {
            HasEnabledUnloadablePlugins = false;
            foreach (var plugin in Plugins)
            {
                plugin.CanLoad = graph.CanLoad(plugin.FileName);
                plugin.MissingMasters = !plugin.CanLoad ?
                    graph.GetMissingMasters(plugin.FileName).ToList().AsReadOnly() : Enumerable.Empty<string>();
                if (plugin.ShouldLoad && !plugin.CanLoad)
                    HasEnabledUnloadablePlugins = true;
            }
        }
    }

    public class PluginSetting : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler Toggled;

        public bool CanLoad { get; set; }
        public string FileName { get; private init; }
        public bool HasMissingMasters => MissingMasters.Any();
        public int Index { get; private init; }
        public bool IsPreviousMerge => FileName == FileStructure.MergeFileName;
        public bool IsReadable { get; private init; }
        public IEnumerable<string> MissingMasters { get; set; } = Enumerable.Empty<string>();
        public string MissingMastersFormatted => string.Join(", ", MissingMasters);
        public bool ShouldLoad { get; set; }

        public PluginSetting(string fileName, int index, bool isReadable, bool defaultEnabled)
        {
            CanLoad = true;
            FileName = fileName;
            Index = index;
            IsReadable = isReadable;
            ShouldLoad = defaultEnabled;
        }

        protected void OnShouldLoadChanged()
        {
            Toggled?.Invoke(this, EventArgs.Empty);
        }
    }

    class NpcInfo<TKey> : IMutableNpc<TKey>
        where TKey : struct
    {
        public string BasePluginName { get; set; }
        public RecordKey DefaultRace { get; set; }
        public string EditorId { get; set; }
        public TKey Key { get; set; }
        public bool IsFemale { get; set; }
        public bool IsSupported { get; set; }
        public string LocalFormIdHex { get; set; }
        public string Name { get; set; }
        public List<NpcOverride<TKey>> Overrides { get; set; } = new List<NpcOverride<TKey>>();

        IReadOnlyList<NpcOverride<TKey>> INpc<TKey>.Overrides => Overrides.AsReadOnly();

        public void AddOverride(NpcOverride<TKey> overrideInfo)
        {
            Overrides.Add(overrideInfo);
        }
    }
}
