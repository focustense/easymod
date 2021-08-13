namespace Focus.ModManagers.ModOrganizer
{
    public interface IModOrganizerConfiguration : IModManagerConfiguration
    {
        string BaseDirectory { get; }
        string DownloadDirectory { get; }
        string OverwriteDirectory { get; }
        string ProfilesDirectory { get; }
        string SelectedProfileName { get; }
    }
}
