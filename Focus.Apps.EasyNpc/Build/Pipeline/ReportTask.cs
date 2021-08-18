﻿using System.Threading.Tasks;

namespace Focus.Apps.EasyNpc.Build.Pipeline
{
    public class ReportTask : BuildTask<BuildReport>
    {
        public delegate ReportTask Factory(PatchSaveTask.Result patch, ArchiveCreationTask.Result archive);

        private readonly IBuildReporter reporter;

        public ReportTask(IBuildReporter reporter, PatchSaveTask.Result patch, ArchiveCreationTask.Result archive)
        {
            RunsAfter(patch, archive);
            this.reporter = reporter;
        }

        protected override Task<BuildReport> Run(BuildSettings settings)
        {
            return Task.Run(() =>
            {
                var report = new BuildReport { ModName = settings.OutputModName };
                reporter.Save(report);
                return report;
            });
        }
    }
}
