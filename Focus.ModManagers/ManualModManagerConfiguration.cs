namespace Focus.ModManagers
{
    public class ManualModManagerConfiguration : IModManagerConfiguration
    {
        public string GameDataPath { get; private init; }
        public string ModsDirectory { get; private init; }

        public ManualModManagerConfiguration(string gamePath, string modsDirectory)
        {
            GameDataPath = gamePath;
            ModsDirectory = modsDirectory;
        }
    }
}
