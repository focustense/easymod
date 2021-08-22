using Focus.Apps.EasyNpc.Configuration;
using System.IO.Abstractions;

namespace Focus.Apps.EasyNpc.Build
{
    public interface IBuildReporter
    {
        void Delete();
        void Save(BuildReport report);
    }

    public class BuildReporter : IBuildReporter
    {
        private readonly IAppSettings appSettings;
        private readonly IFileSystem fs;

        public BuildReporter(IFileSystem fs, IAppSettings appSettings)
        {
            this.appSettings = appSettings;
            this.fs = fs;
        }

        public void Delete()
        {
            fs.File.Delete(appSettings.BuildReportPath);
        }

        public void Save(BuildReport report)
        {
            using var stream = fs.File.Create(appSettings.BuildReportPath);
            report.SaveToStream(stream);
        }
    }
}
