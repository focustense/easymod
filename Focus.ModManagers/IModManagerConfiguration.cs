namespace Focus.ModManagers
{
    public interface IModManagerConfiguration
    {
        public string GameDataPath { get; }
        public string ModsDirectory { get; }
    }
}
