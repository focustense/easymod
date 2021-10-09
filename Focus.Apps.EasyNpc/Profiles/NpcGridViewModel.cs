using PropertyChanged;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Focus.Apps.EasyNpc.Profiles
{
    public class NpcGridViewModel : INotifyPropertyChanged
    {
#pragma warning disable 67 // Implemented by PropertyChanged.Fody
        public event PropertyChangedEventHandler? PropertyChanged;
#pragma warning restore 67

        public NpcGridHeaders Headers { get; private set; }
        public IEnumerable<INpc> Npcs { get; set; } = Enumerable.Empty<Npc>();
        public INpc? SelectedNpc { get; set; }

        public NpcGridViewModel(NpcSearchParameters searchParameters)
        {
            Headers = new(searchParameters);
        }
    }

    [DoNotNotify]
    public class NpcGridHeaders : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public LabeledValue<string> BasePluginName { get; private init; }
        public LabeledValue<string> EditorId { get; private init; }
        public LabeledValue<string> LocalFormIdHex { get; private init; }
        public LabeledValue<string> Name { get; private init; }

        public NpcGridHeaders(NpcSearchParameters searchParameters)
        {
            BasePluginName = new LabeledValue<string>(
                "Base Plugin", () => searchParameters.BasePluginName, v => searchParameters.BasePluginName = v);
            EditorId = new LabeledValue<string>(
                "Editor ID", () => searchParameters.EditorId, v => searchParameters.EditorId = v);
            LocalFormIdHex = new LabeledValue<string>(
                "Form ID", () => searchParameters.LocalFormIdHex, v => searchParameters.LocalFormIdHex = v);
            Name = new LabeledValue<string>(
                "Name", () => searchParameters.Name, v => searchParameters.Name = v);

            searchParameters.PropertyChanged += (_, e) => PropertyChanged?.Invoke(this, e);
        }
    }
}
