using PropertyChanged;
using System;
using System.ComponentModel;

namespace Focus.Apps.EasyNpc.Profiles
{
    [AddINotifyPropertyChangedInterface]
    public class LabeledValue<T>
    {
        public string Label { get; set; } = string.Empty;

        public T Value
        {
            get { return getValue(); }
            set { setValue(value); }
        }

        private readonly Func<T> getValue;
        private readonly Action<T> setValue;

        public LabeledValue(string label, Func<T> getValue, Action<T> setValue)
            : this(getValue, setValue)
        {
            Label = label;
        }

        public LabeledValue(Func<T> getValue, Action<T> setValue)
        {
            this.getValue = getValue;
            this.setValue = setValue;
        }
    }
}
