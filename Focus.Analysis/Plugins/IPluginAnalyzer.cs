namespace Focus.Analysis.Plugins
{
    public interface IPluginAnalyzer
    {
        PluginAnalysis Analyze(string pluginName);
    }
}