using System.Collections.Generic;

namespace Focus.Apps.EasyNpc.Configuration
{
    public interface IAppSettings
    {
        string BuildReportPath { get; }
        IEnumerable<BuildWarningSuppression> BuildWarningWhitelist { get; }
        string ModRootDirectory { get; }
        IEnumerable<MugshotRedirect> MugshotRedirects { get; }
        string MugshotsDirectory { get; }
    }
}