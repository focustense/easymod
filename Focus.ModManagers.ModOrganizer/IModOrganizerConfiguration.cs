namespace Focus.ModManagers.ModOrganizer
{
    public interface IModOrganizerConfiguration
    {
        string BaseDirectory { get; }
        string DownloadDirectory { get; }
        string ModsDirectory { get; }
        string OverwriteDirectory { get; }
        string ProfilesDirectory { get; }
    }
}
