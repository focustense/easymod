using System;
using System.Collections.Generic;
using System.Linq;
using Focus.Analysis.Execution;
using Focus.Analysis.Plugins;
using PropertyChanged;

namespace Focus.Apps.EasyNpc.Reports
{
    [AddINotifyPropertyChangedInterface]
    public class PluginErrorViewModel
    {
        public string ErrorMessage => $"{Exception.GetType().Name}: {Exception.Message}";
        public Exception Exception { get; private init; }
        public string PluginName { get; private init; }

        public PluginErrorViewModel(string pluginName, Exception exception)
        {
            PluginName = pluginName;
            Exception = exception;
        }
    }

    [AddINotifyPropertyChangedInterface]
    public class PluginErrorsViewModel
    {
        public IReadOnlyList<PluginErrorViewModel> Plugins { get; private init; }

        public PluginErrorsViewModel(LoadOrderAnalysis analysis)
            : this(analysis.Plugins) { }

        public PluginErrorsViewModel(IEnumerable<PluginAnalysis> pluginAnalyses)
        {
            Plugins = pluginAnalyses
                .Where(x => x.Exception is not null)
                .Select(x => new PluginErrorViewModel(x.FileName, x.Exception!))
                .ToList()
                .AsReadOnly();
        }
    }
}
