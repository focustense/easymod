using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Focus.Apps.EasyNpc.Reports
{
    public enum SummaryItemCategory
    {
        None,
        CountEmpty,
        CountExcluded,
        CountIncluded,
        CountFull,
        CountUnavailable,
        SpecialFlag,
        StatusError,
        StatusInfo,
        StatusOk,
        StatusWarning,
    }

    public class SummaryItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public SummaryItemCategory Category { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? Value { get; set; }

        public SummaryItem()
        {
        }

        public SummaryItem(SummaryItemCategory category, string description, string? value = null)
        {
            Category = category;
            Description = description;
            Value = value;
        }

        public SummaryItem(SummaryItemCategory category, string description, Decimal value)
            : this(category, description, value.ToString())
        { }
    }
}
