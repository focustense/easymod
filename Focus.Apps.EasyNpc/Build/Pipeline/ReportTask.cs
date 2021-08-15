using System.Threading.Tasks;

namespace Focus.Apps.EasyNpc.Build.Pipeline
{
    public class ReportTask : BuildTask<BuildReport>
    {
        public override string Name => "Report Results";

        public delegate ReportTask Factory(PatchSaveTask.Result patch, ArchiveCreationTask.Result archive);

        public ReportTask(PatchSaveTask.Result patch, ArchiveCreationTask.Result archive)
        {
            RunsAfter(patch, archive);
        }

        protected override Task<BuildReport> Run(BuildSettings settings)
        {
            return Task.FromResult(new BuildReport { ModName = settings.OutputModName });
        }
    }
}
