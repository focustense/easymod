namespace Focus.Environment
{
    public interface ILoadOrderGraph : IReadOnlyLoadOrderGraph
    {
        void SetEnabled(string pluginName, bool enabled);
    }
}