namespace Focus.Apps.EasyNpc.Messages
{
    public class SettingsChanged
    {
        public enum SettingKind
        {
            BuildWarnings,
            DefaultModDirectory,
            ModDirectorySource,
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
