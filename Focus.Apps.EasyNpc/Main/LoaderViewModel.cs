using Focus.Apps.EasyNpc.Debug;
using Focus.Apps.EasyNpc.GameData.Files;
using Focus.Environment;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Focus.Apps.EasyNpc.Main
{
    [AddINotifyPropertyChangedInterface]
    public class LoaderViewModel
    {
        public event Action? Loaded;

        public TaskViewModel AnalysisTask { get; private init; } = new("Analysis");
        public bool CanLoad { get; private set; }
        public bool HasEnabledUnloadablePlugins { get; private set; }
        public bool IsLoading { get; private set; }
        public bool IsLogVisible { get; private set; }
        public bool IsPluginListVisible { get; private set; }
        [DependsOn("HasEnabledUnloadablePlugins", "IsPluginListVisible")]
        public bool IsUnloadablePluginWarningVisible => HasEnabledUnloadablePlugins && IsPluginListVisible;
        public LogViewModel Log { get; private init; }
        public TaskViewModel ModIndexTask { get; private init; } = new("Mod Scan");
        public IReadOnlyList<PluginSetting> Plugins { get; private set; }
        public TaskViewModel ProfileTask { get; private init; } = new("Profile");
        public LoaderTasks? Tasks { get; private set; }

        private readonly LoaderModel loader;
        private readonly IGameSetup setup;

        public LoaderViewModel(LogViewModel logViewModel, IGameSetup setup, LoaderModel loader)
        {
            this.loader = loader;
            this.setup = setup;

            Log = logViewModel;

            CanLoad = false;
            loader.Prepare();

            Plugins = setup.AvailablePlugins
                .Select((x, i) => new PluginSetting(x.FileName, i + 1, x.IsReadable, x.IsEnabled))
                .ToList()
                .AsReadOnly();
            UpdatePluginStates();
            foreach (var plugin in Plugins)
                plugin.Toggled += Plugin_Toggled;
            IsPluginListVisible = true;
            CanLoad = true;
        }

        public async void ConfirmPlugins()
        {
            CanLoad = false;
            IsLoading = true;
            IsPluginListVisible = false;
            IsLogVisible = true;

            Tasks = loader.Complete();
            AnalysisTask.SetTask(Tasks.LoadOrderAnalysis);
            ModIndexTask.SetTask(Tasks.ModRepository);
            ProfileTask.SetTask(Tasks.Profile);
            await Task.WhenAll(new Task[] { Tasks.ModRepository, Tasks.LoadOrderAnalysis, Tasks.Profile })
                .ConfigureAwait(false);
            
            Loaded?.Invoke();
            IsLoading = false;
        }

        public void TogglePlugins(IEnumerable<PluginSetting> plugins)
        {
            var materializedPlugins = plugins.ToList();
            if (materializedPlugins.Count == 0)
                return;
            var shouldLoad = materializedPlugins.Any(x => !x.ShouldLoad && x.CanLoad);
            // If enabling plugins, iteration should be sensitive to side effects - i.e. "CanLoad" may be false due to
            // disabled master, but if the master is also being enabled, then "CanLoad" may change to true during
            // iteration. When disabling plugins, however, we want the exact opposite of this - all of the plugins that
            // were selected should each be disabled, regardless of whether it becomes unloadable during iteration.
            foreach (var plugin in materializedPlugins)
                if (plugin.CanLoad || !shouldLoad) // Row shouldn't be editable otherwise
                    plugin.ShouldLoad = shouldLoad;
        }

        private void Plugin_Toggled(object? sender, EventArgs e)
        {
            if (sender is not PluginSetting pluginSetting)
                return;
            setup.LoadOrderGraph.SetEnabled(pluginSetting.FileName, pluginSetting.ShouldLoad);
            UpdatePluginStates();
        }

        private void UpdatePluginStates()
        {
            HasEnabledUnloadablePlugins = false;
            foreach (var plugin in Plugins)
            {
                plugin.CanLoad = setup.LoadOrderGraph.CanLoad(plugin.FileName);
                plugin.MissingMasters = !plugin.CanLoad ?
                    setup.LoadOrderGraph.GetMissingMasters(plugin.FileName).ToList().AsReadOnly() :
                    Enumerable.Empty<string>();
                if (plugin.ShouldLoad && !plugin.CanLoad)
                    HasEnabledUnloadablePlugins = true;
            }
        }
    }

    [AddINotifyPropertyChangedInterface]
    public class PluginSetting
    {
        public event EventHandler? Toggled;

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
}
