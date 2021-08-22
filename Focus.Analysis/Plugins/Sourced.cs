namespace Focus.Analysis.Plugins
{
    public class Sourced<T>
    {
        public T Analysis { get; private init; }
        public string PluginName { get; private init; }

        public Sourced(string pluginName, T analysis)
        {
            Analysis = analysis;
            PluginName = pluginName;
        }
    }
}
