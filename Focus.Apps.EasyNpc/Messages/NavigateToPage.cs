namespace Focus.Apps.EasyNpc.Messages
{
    public enum MainPage
    {
        Profile,
        Build,
        BuildPreview,
        Maintenance,
        Log,
        Settings
    }

    public class NavigateToPage
    {
        public MainPage Page { get; private init; }

        public NavigateToPage(MainPage page)
        {
            Page = page;
        }
    }
}