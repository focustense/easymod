using Focus.Apps.EasyNpc.GameData.Files;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Focus.Apps.EasyNpc.Reports
{
    [AddINotifyPropertyChangedInterface]
    public class PostBuildReportViewModel
    {
        [DependsOn(nameof(FaceGenArchiveExtractor))]
        public bool CanExtractFaceGenFiles => FaceGenArchiveExtractor is not null;
        [DependsOn(nameof(FaceTintArchiveExtractor))]
        public bool CanExtractFaceTintFiles => FaceTintArchiveExtractor is not null;
        public ArchiveExtractorViewModel? FaceGenArchiveExtractor { get; private set; }
        public ArchiveExtractorViewModel? FaceTintArchiveExtractor { get; private set; }
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
        public IEnumerable<NpcConsistencyInfo> InconsistentFaceTintNpcs =>
            Report.Npcs.Where(x => !x.HasConsistentFaceTint);
        [DependsOn(nameof(Report))]
        public IEnumerable<NpcConsistencyInfo> InconsistentHeadPartNpcs =>
            Report.Npcs.Where(x => !x.HasConsistentHeadParts);
        [DependsOn(nameof(FaceGenArchiveExtractor))]
        public bool IsFaceGenArchiveExtractionStarted => FaceGenArchiveExtractor is not null;
        [DependsOn(nameof(FaceTintArchiveExtractor))]
        public bool IsFaceTintArchiveExtractionStarted => FaceTintArchiveExtractor is not null;
        [DependsOn(nameof(Report))]
        public bool IsMainPluginEnabled => Report.MainPluginState == PluginState.Enabled;
        public bool IsReportReady { get; private set; }
        [DependsOn(nameof(Report))]
        public string? MergeComponentName => Report.ActiveMergeComponents.FirstOrDefault()?.Name;
        public PostBuildReport Report { get; private set; } = new();

        private readonly ArchiveExtractorViewModel.Factory archiveExtractorFactory;
        private readonly PostBuildReportGenerator reportGenerator;

        public PostBuildReportViewModel(
            PostBuildReportGenerator reportGenerator, ArchiveExtractorViewModel.Factory archiveExtractorFactory)
        {
            this.archiveExtractorFactory = archiveExtractorFactory;
            this.reportGenerator = reportGenerator;

            reportGenerator.Status.Subscribe(s => GenerationStatus = s);
        }

        public async Task UpdateReport()
        {
            IsReportReady = false;
            Report = await reportGenerator.CreateReport();
            if (HasSingleMergeComponent)
            {
                var mergeComponent = Report.ActiveMergeComponents[0];
                var faceGenFiles = InconsistentHeadPartNpcs
                    .Where(x => x.HasFaceGenArchive && !x.HasFaceGenLoose)
                    .Select(x => new ArchiveFile(x.FaceGenArchivePath!, FileStructure.GetFaceMeshFileName(x)));
                var faceGenTintFiles = InconsistentHeadPartNpcs
                    .Where(x => x.HasFaceTintArchive && !x.HasFaceTintLoose)
                    .Select(x => new ArchiveFile(x.FaceTintArchivePath!, FileStructure.GetFaceTintFileName(x)));
                if (faceGenFiles.Any())
                    FaceGenArchiveExtractor =
                        archiveExtractorFactory(faceGenFiles.Concat(faceGenTintFiles), mergeComponent);

                var faceTintFiles = InconsistentFaceTintNpcs
                    .Where(x => x.HasFaceTintArchive && !x.HasFaceTintLoose)
                    .Select(x => new ArchiveFile(x.FaceTintArchivePath!, FileStructure.GetFaceTintFileName(x)));
                if (faceTintFiles.Any())
                    FaceTintArchiveExtractor = archiveExtractorFactory(faceTintFiles, mergeComponent);
            }
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
