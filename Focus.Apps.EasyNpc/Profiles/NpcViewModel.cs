using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Focus.Apps.EasyNpc.Profiles
{
    public class NpcViewModel : INotifyPropertyChanged, INpcBasicInfo
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public string BasePluginName => npc.BasePluginName;
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

        private readonly Npc npc;

        public NpcViewModel(Npc npc, IAsyncEnumerable<Mugshot> mugshots)
        {
            this.npc = npc;

            Options = npc.Options.Select(x => CreateOption(x)).ToList().AsReadOnly();
            DefaultOption = GetOption(npc.DefaultOption.PluginName);
            FaceOption = GetOption(npc.FaceOption.PluginName);
            Mugshots = mugshots.Select(x => new MugshotViewModel(x, FaceOptionIsAny(x.InstalledPlugins)))
                .ToObservableCollection();
        }

        public bool TrySetDefaultPlugin(string pluginName, [MaybeNullWhen(false)] out NpcOptionViewModel option)
        {
            var success = npc.SetDefaultOption(pluginName) == Npc.ChangeResult.OK;
            if (success)
                DefaultOption = option = GetOption(npc.DefaultOption.PluginName);
            else
                option = null;
            return success;
        }

        public bool TrySetFaceMod(string modName, [MaybeNullWhen(false)] out NpcOptionViewModel option)
        {
            var success = npc.SetFaceMod(modName) == Npc.ChangeResult.OK;
            if (success)
            {
                // If the selected mod ends up being an override (no option/plugin), the "option" parameter will be the
                // previously-selected option, and the FaceOption won't change. This is the expected behavior. There is
                // no guarantee that the output option always corresponds to the mod name.
                FaceOption = option = GetOption(npc.FaceOption.PluginName);
                FaceModNames = npc.GetFaceModNames().ToHashSet(StringComparer.CurrentCultureIgnoreCase);
            }
            else
                option = null;
            return success;
        }

        public bool TrySetFacePlugin(string pluginName, [MaybeNullWhen(false)] out NpcOptionViewModel option)
        {
            var success = npc.SetFaceOption(pluginName) == Npc.ChangeResult.OK;
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
            foreach (var mugshot in Mugshots)
                mugshot.IsHighlighted = false;
            UpdateHighlights(SelectedMugshot?.InstalledPlugins ?? Enumerable.Empty<string>());
        }

        protected void OnSelectedOptionChanged(object before, object after)
        {
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

        private bool FaceOptionIsAny(IEnumerable<string> pluginNames)
        {
            return
                FaceOption is not null &&
                pluginNames.Contains(FaceOption.PluginName, StringComparer.CurrentCultureIgnoreCase);
        }

        private NpcOptionViewModel GetOption(string pluginName)
        {
            // The way in which this method is used should always succeed - i.e. we get the plugin name from the model
            // itself, and then use it to find the option we generated from the model's options.
            return Options.Single(x => x.PluginName == pluginName);
        }

        private void Option_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is not NpcOptionViewModel option)
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
                mugshot.IsSelectedSource = FaceOptionIsAny(mugshot.InstalledPlugins);
        }
    }
}
