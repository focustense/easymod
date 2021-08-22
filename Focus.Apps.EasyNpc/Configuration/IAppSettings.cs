using System;
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
        string StaticAssetsPath { get; }
    }

    public interface IObservableAppSettings : IAppSettings
    {
        IObservable<IReadOnlyList<BuildWarningSuppression>> BuildWarningWhitelistObservable { get; }
        IObservable<string> ModRootDirectoryObservable { get; }
        IObservable<IReadOnlyList<MugshotRedirect>> MugshotRedirectsObservable { get; }
        IObservable<string> MugshotsDirectoryObservable { get; }
    }
}