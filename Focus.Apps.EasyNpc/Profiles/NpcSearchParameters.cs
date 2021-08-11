#nullable enable

using System.ComponentModel;

namespace Focus.Apps.EasyNpc.Profiles
{
    public class NpcSearchParameters : INpcSearchParameters, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public string BasePluginName { get; set; } = string.Empty;
        public string EditorId { get; set; } = string.Empty;
        public string LocalFormIdHex { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;

        public void Reset()
        {
            BasePluginName = string.Empty;
            EditorId = string.Empty;
            LocalFormIdHex = string.Empty;
            Name = string.Empty;
        }
    }
}
