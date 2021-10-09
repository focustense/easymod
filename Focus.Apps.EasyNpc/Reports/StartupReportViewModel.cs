using Focus.Analysis.Execution;
using Focus.Analysis.Plugins;
using Focus.Apps.EasyNpc.Profiles;
using System;

namespace Focus.Apps.EasyNpc.Reports
{
    public class StartupReportViewModel
    {
        public delegate StartupReportViewModel Factory(Profile profile, LoadOrderAnalysis analysis);

        public bool HasErrors => HasInvalidReferences || HasPluginErrors;
        public bool HasInvalidReferences => InvalidReferences.Items.Count > 0;
        public bool HasPluginErrors => PluginErrors.Plugins.Count > 0;

        public InvalidReferencesViewModel InvalidReferences { get; private init; }
        public PluginErrorsViewModel PluginErrors { get; private init; }

        public StartupReportViewModel(
            InvalidReferencesViewModel.Factory invalidReferencesFactory, Profile profile, LoadOrderAnalysis analysis)
        {
            InvalidReferences = invalidReferencesFactory(profile);
            PluginErrors = new(analysis);
        }
    }
}
