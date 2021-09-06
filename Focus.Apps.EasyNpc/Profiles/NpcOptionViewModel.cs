using Focus.Analysis.Records;
using System;
using System.ComponentModel;

namespace Focus.Apps.EasyNpc.Profiles
{
    public class NpcOptionViewModel : INotifyPropertyChanged
    {
#pragma warning disable 67 // Implemented by PropertyChanged.Fody
        public event PropertyChangedEventHandler? PropertyChanged;
#pragma warning restore 67

        public bool HasBehaviorOverride => ComparisonToBase?.ModifiesBehavior ?? true;
        public bool HasErrors => option.HasErrors;
        public bool HasFaceOverride => ComparisonToBase?.ModifiesFace ?? true;
        public bool HasFaceGenOverride => ComparisonToBase?.ModifiesHeadParts ?? true;
        public bool HasOutfitOverride => ComparisonToBase?.ModifiesOutfits ?? true;
        public bool HasSkinOverride => ComparisonToBase?.ModifiesSkin ?? true;
        public bool HasWig => option.Analysis.WigInfo != null;
        public bool IsDefaultSource { get; set; }
        public bool IsFaceSource { get; set; }
        public bool IsHighlighted { get; set; }
        public bool IsSelected { get; set; }
        public string PluginName => option.PluginName;

        protected NpcComparison? ComparisonToBase => option.Analysis.ComparisonToBase;

        private readonly NpcOption option;

        public NpcOptionViewModel(NpcOption option)
        {
            this.option = option;
        }

        public bool PluginEquals(string pluginName)
        {
            return PluginName.Equals(pluginName, StringComparison.CurrentCultureIgnoreCase);
        }
    }
}
