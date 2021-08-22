namespace Focus.ModManagers
{
    public class ManualModManagerConfiguration : IModManagerConfiguration
    {
        public string ModsDirectory { get; private init; }

        public ManualModManagerConfiguration(string modsDirectory)
        {
            ModsDirectory = modsDirectory;
        }
    }
}
