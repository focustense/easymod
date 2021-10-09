using PropertyChanged;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Focus.Apps.EasyNpc.Profiles
{
    public class NpcViewModel : INotifyPropertyChanged, INpcBasicInfo
    {
#pragma warning disable 67 // Implemented by PropertyChanged.Fody
        public event PropertyChangedEventHandler? PropertyChanged;
#pragma warning restore 67

        public string BasePluginName => npc.BasePluginName;
        [DependsOn(nameof(DefaultOption))]
        public bool CanCustomizeFace => npc.CanCustomizeFace;
        public NpcOptionViewModel DefaultOption { get; private set; }
        public string DescriptiveLabel => $"'{Name}' ({BasePluginName} - {EditorId})";
        public string EditorId => npc.EditorId;
        public string ExtendedFormId => $"{BasePluginName}#{LocalFormIdHex}";
        // Face mod names are either all of the mod names corresponding to the selected face option (plugin), OR the one
        // mod chosen as a facegen override, i.e. if it doesn't have a plugin.
        public IReadOnlySet<string> FaceModNames { get; private set; } = new HashSet<string>();
        public NpcOptionViewModel FaceOption { get; private set; }
        public IRecordKey Key => npc;
        public bool IsFemale => npc.IsFemale;
        public string LocalFormIdHex => npc.LocalFormIdHex;
        public string? MissingDefaultPluginName => npc.MissingDefaultPluginName;
        public string? MissingFacePluginName => npc.MissingFacePluginName;
        public IReadOnlyList<MugshotViewModel> Mugshots { get; private set; } =
            new List<MugshotViewModel>().AsReadOnly();
        public string Name => npc.Name;
        public IReadOnlyList<NpcOptionViewModel> Options { get; private init; }
        public MugshotViewModel? SelectedMugshot { get; set; }
        // Selected option refers to selection in the UI view, and has no effect on the profile itself.
        public NpcOptionViewModel? SelectedOption { get; set; }

        private readonly bool isInitialized;
        private readonly INpc npc;

        public NpcViewModel(INpc npc, IAsyncEnumerable<Mugshot> mugshots)
        {
            this.npc = npc;

            Options = npc.Options.Select(x => CreateOption(x)).ToList().AsReadOnly();
            DefaultOption = GetOption(npc.DefaultOption.PluginName);
            FaceOption = GetOption(npc.FaceOption.PluginName);
            Mugshots = mugshots
                .Select(x => new MugshotViewModel(x, npc.Options, IsSelectedMugshot(x.ModName, x.InstalledPlugins)))
                .ToObservableCollection();
            isInitialized = true;
        }

        public bool TrySetDefaultPlugin(string pluginName, [MaybeNullWhen(false)] out NpcOptionViewModel option)
        {
            var success = npc.SetDefaultOption(pluginName) == NpcChangeResult.OK;
            if (success)
                DefaultOption = option = GetOption(npc.DefaultOption.PluginName);
            else
                option = null;
            return success;
        }

        public bool TrySetFaceMod(MugshotViewModel mugshot, [MaybeNullWhen(false)] out NpcOptionViewModel option)
        {
            return TrySetFaceMod(mugshot.ModName, mugshot.IsBaseGame, out option);
        }

        public bool TrySetFaceMod(string modName, [MaybeNullWhen(false)] out NpcOptionViewModel option)
        {
            return TrySetFaceMod(modName, false, out option);
        }

        public bool TrySetFacePlugin(string pluginName, [MaybeNullWhen(false)] out NpcOptionViewModel option)
        {
            var success = npc.SetFaceOption(pluginName) == NpcChangeResult.OK;
            if (success)
            {
                FaceOption = option = GetOption(npc.FaceOption.PluginName);
                FaceModNames = npc.GetFaceModNames().ToHashSet(StringComparer.CurrentCultureIgnoreCase);
            }
            else
                option = null;
            return success;
        }

        protected void OnDefaultOptionChanged(object before, object after)
        {
            if (before is NpcOptionViewModel previousOption)
                previousOption.IsDefaultSource = false;
            if (after is NpcOptionViewModel currentOption)
                currentOption.IsDefaultSource = true;
        }

        protected void OnFaceOptionChanged(object before, object after)
        {
            if (before is NpcOptionViewModel previousOption)
                previousOption.IsFaceSource = false;
            if (after is NpcOptionViewModel currentOption)
                currentOption.IsFaceSource = true;
            UpdateMugshotAssignments();
        }

        protected void OnSelectedMugshotChanged()
        {
            if (!isInitialized)
                return;
            foreach (var mugshot in Mugshots)
                mugshot.IsHighlighted = false;
            UpdateHighlights(SelectedMugshot?.InstalledPlugins ?? Enumerable.Empty<string>());
        }

        protected void OnSelectedOptionChanged(object before, object after)
        {
            if (!isInitialized)
                return;
            foreach (var option in Options)
                option.IsHighlighted = false;
            if (before is NpcOptionViewModel previousOption)
                previousOption.IsSelected = false;
            if (after is NpcOptionViewModel currentOption)
                currentOption.IsSelected = true;
            UpdateHighlights(SelectedOption?.PluginName);
        }

        private NpcOptionViewModel CreateOption(NpcOption option)
        {
            var optionViewModel = new NpcOptionViewModel(option);
            optionViewModel.PropertyChanged += Option_PropertyChanged;
            return optionViewModel;
        }

        private NpcOptionViewModel GetOption(string pluginName)
        {
            // The way in which this method is used should always succeed - i.e. we get the plugin name from the model
            // itself, and then use it to find the option we generated from the model's options.
            return Options.Single(x => x.PluginName == pluginName);
        }

        private bool IsSelectedMugshot(string modName, IEnumerable<string> installedPlugins)
        {
            if (npc.FaceGenOverride is not null)
                return npc.FaceGenOverride.IncludesName(modName);
            return
                FaceOption is not null &&
                installedPlugins.Contains(FaceOption.PluginName, StringComparer.CurrentCultureIgnoreCase);
        }

        private void Option_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (!isInitialized || sender is not NpcOptionViewModel option)
                return;
            switch (e.PropertyName)
            {
                case nameof(NpcOptionViewModel.IsSelected):
                    if (option.IsSelected)
                        SelectedOption = option;
                    break;
                case nameof(NpcOptionViewModel.IsDefaultSource):
                    if (option.IsDefaultSource)
                        TrySetDefaultPlugin(option.PluginName, out _);
                    break;
                case nameof(NpcOptionViewModel.IsFaceSource):
                    if (option.IsFaceSource)
                        TrySetFacePlugin(option.PluginName, out _);
                    break;
            }
        }

        private bool TrySetFaceMod(
            string modName, bool isBaseGame, [MaybeNullWhen(false)] out NpcOptionViewModel option)
        {
            var result = isBaseGame ? npc.RevertToBaseGame() : npc.SetFaceMod(modName);
            var success = result == NpcChangeResult.OK;
            if (success)
            {
                // If the selected mod ends up being an override (no option/plugin), the "option" parameter will be the
                // previously-selected option, and the FaceOption won't change. This is the expected behavior. There is
                // no guarantee that the output option always corresponds to the mod name.
                option = GetOption(npc.FaceOption.PluginName);
                if (FaceOption != option)
                    FaceOption = option;
                else
                    // Change handler won't run if they're the same, need to explicitly update mugshot states.
                    UpdateMugshotAssignments();
                FaceModNames = npc.GetFaceModNames().ToHashSet(StringComparer.CurrentCultureIgnoreCase);
            }
            else
                option = null;
            return success;
        }

        private void UpdateHighlights(string? pluginName)
        {
            var pluginNames = pluginName is not null ? new[] { pluginName } : new string[0];
            UpdateHighlights(pluginNames);
        }

        private void UpdateHighlights(IEnumerable<string> pluginNames)
        {
            var pluginNameSet = pluginNames.ToHashSet(StringComparer.CurrentCultureIgnoreCase);
            foreach (var mugshot in Mugshots)
                mugshot.IsHighlighted = mugshot.InstalledPlugins.Any(p => pluginNameSet.Contains(p));
            foreach (var option in Options)
                option.IsHighlighted = pluginNameSet.Contains(option.PluginName);
        }

        private void UpdateMugshotAssignments()
        {
            foreach (var mugshot in Mugshots)
                mugshot.IsSelectedSource = IsSelectedMugshot(mugshot.ModName, mugshot.InstalledPlugins);
        }
    }
}
