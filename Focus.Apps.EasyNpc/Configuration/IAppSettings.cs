using System;
using System.Collections.Generic;

namespace Focus.Apps.EasyNpc.Configuration
{
    public interface IAppSettings
    {
        string BuildReportPath { get; }
        IEnumerable<BuildWarningSuppression> BuildWarningWhitelist { get; }
        string DefaultModRootDirectory { get; }
        IEnumerable<MugshotRedirect> MugshotRedirects { get; }
        string MugshotsDirectory { get; }
        IEnumerable<IRecordKey> RaceTransformationKeys { get; }
        string StaticAssetsPath { get; }
        bool UseModManagerForModDirectory { get; }
    }

    public interface IMutableAppSettings
    {
        IReadOnlyList<BuildWarningSuppression> BuildWarningWhitelist { get; set; }
        string DefaultModRootDirectory { get; set; }
        IReadOnlyList<MugshotRedirect> MugshotRedirects { get; set; }
        string MugshotsDirectory { get; set; }
        bool UseModManagerForModDirectory { get; set; }

        void Save();
    }

    public interface IObservableAppSettings : IAppSettings
    {
        IObservable<IReadOnlyList<BuildWarningSuppression>> BuildWarningWhitelistObservable { get; }
        IObservable<string> DefaultModRootDirectoryObservable { get; }
        IObservable<IReadOnlyList<MugshotRedirect>> MugshotRedirectsObservable { get; }
        IObservable<string> MugshotsDirectoryObservable { get; }
        IObservable<bool> UseModManagerForModDirectoryObservable { get; }
    }
}