namespace Focus.Apps.EasyNpc.Messages
{
    public class SettingsChanged
    {
        public enum SettingKind
        {
            BuildWarnings,
            ModDirectory,
            MugshotDirectory,
            MugshotSynonyms
        };

        public SettingKind Setting { get; private init; }

        public SettingsChanged(SettingKind setting)
        {
            Setting = setting;
        }
    }
}
