using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Focus.Apps.EasyNpc
{
    public class DataGridComboBoxColumn : DataGridTextColumn
    {
        public Binding? ItemsSource { get; set; }

        protected override FrameworkElement GenerateEditingElement(DataGridCell cell, object dataItem)
        {
            var comboBox = new ComboBox
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                IsEditable = true,
                VerticalAlignment = VerticalAlignment.Stretch,
            };
            BindingOperations.SetBinding(comboBox, ItemsControl.ItemsSourceProperty, ItemsSource);
            BindingOperations.SetBinding(comboBox, ComboBox.TextProperty, Binding);
            return comboBox;
        }

        protected override object? PrepareCellForEdit(FrameworkElement editingElement, RoutedEventArgs editingEventArgs)
        {
            if (editingElement is not ComboBox comboBox)
                return null;
            comboBox.Focus();
            return comboBox.Text;
        }
    }
}