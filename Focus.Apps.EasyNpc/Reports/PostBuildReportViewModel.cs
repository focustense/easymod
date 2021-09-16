using PropertyChanged;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Focus.Apps.EasyNpc.Reports
{
    [AddINotifyPropertyChangedInterface]
    public class PostBuildReportViewModel
    {
        public string GenerationStatus { get; private set; } = string.Empty;
        [DependsOn(nameof(Report))]
        public bool HasAllArchives => Report.Archives.All(x => !string.IsNullOrEmpty(x.ArchiveName));
        [DependsOn(nameof(Report))]
        public bool HasAllDummyPluginsEnabled =>
            Report.Archives.All(x => !x.RequiresDummyPlugin || x.DummyPluginState == PluginState.Enabled);
        [DependsOn(nameof(Report))]
        public bool HasAllReadableArchives =>
            Report.Archives.All(x => !string.IsNullOrEmpty(x.ArchiveName) && x.IsReadable);
        [DependsOn(nameof(Report))]
        public bool HasIssues => GetHasIssues();
        [DependsOn(nameof(Report))]
        public bool HasMultipleMergeComponents => Report.ActiveMergeComponents.Count > 1;
        [DependsOn(nameof(Report))]
        public bool HasSingleMergeComponent => Report.ActiveMergeComponents.Count == 1;
        [DependsOn(nameof(Report))]
        public bool HasConsistentFaceGens => Report.Npcs.All(x => x.HasConsistentHeadParts);
        [DependsOn(nameof(Report))]
        public bool HasConsistentFaceTints => Report.Npcs.All(x => x.HasConsistentFaceTint);
        [DependsOn(nameof(Report))]
        public bool IsMainPluginEnabled => Report.MainPluginState == PluginState.Enabled;
        public bool IsReportReady { get; private set; }
        [DependsOn(nameof(Report))]
        public string? MergeComponentName => Report.ActiveMergeComponents.FirstOrDefault()?.Name;
        public PostBuildReport Report { get; private set; } = new();

        private readonly PostBuildReportGenerator reportGenerator;

        public PostBuildReportViewModel(PostBuildReportGenerator reportGenerator)
        {
            this.reportGenerator = reportGenerator;

            reportGenerator.Status.Subscribe(s => GenerationStatus = s);
        }

        public async Task UpdateReport()
        {
            IsReportReady = false;
            Report = await reportGenerator.CreateReport();
            IsReportReady = true;
        }

        private bool GetHasIssues()
        {
            return
                !IsMainPluginEnabled ||
                !HasSingleMergeComponent ||
                !HasAllDummyPluginsEnabled ||
                !HasAllArchives ||
                !HasAllReadableArchives ||
                Report.Npcs.Any(x => !x.HasConsistentHeadParts || !x.HasConsistentFaceTint);
        }
    }
}
