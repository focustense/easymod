namespace Focus.Apps.EasyNpc.GameData.Plugins
{
    public interface ILoadOrderGraph : IReadOnlyLoadOrderGraph
    {
        void SetEnabled(string pluginName, bool enabled);
    }
}