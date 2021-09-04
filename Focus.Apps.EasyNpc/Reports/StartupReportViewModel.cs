using Focus.Apps.EasyNpc.Profiles;

namespace Focus.Apps.EasyNpc.Reports
{
    public class StartupReportViewModel
    {
        public delegate StartupReportViewModel Factory(Profile profile);

        public bool HasErrors => HasInvalidReferences;
        public bool HasInvalidReferences => InvalidReferences.Items.Count > 0;

        public InvalidReferencesViewModel InvalidReferences { get; private init; }

        public StartupReportViewModel(Profile profile)
        {
            InvalidReferences = new(profile);
        }
    }
}
