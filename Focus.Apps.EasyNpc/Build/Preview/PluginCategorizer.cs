using Focus.Apps.EasyNpc.Profiles;
using Focus.Apps.EasyNpc.Resources;
using Focus.Environment;
using System.ComponentModel;
using System.Linq;

namespace Focus.Apps.EasyNpc.Build.Preview
{
    public enum PluginCategory
    {
        [LocalizedDescription(typeof(EnumResources), "PluginCategory_Unknown")]
        Unknown = 0,
        [LocalizedDescription(typeof(EnumResources), "PluginCategory_BaseGame")]
        BaseGame,
        [LocalizedDescription(typeof(EnumResources), "PluginCategory_NpcOverhaul")]
        NpcOverhaul,
        [LocalizedDescription(typeof(EnumResources), "PluginCategory_NpcOverhaulPatch")]
        NpcOverhaulPatch,
        [LocalizedDescription(typeof(EnumResources), "PluginCategory_Other")]
        Other,
    }

    public interface IPluginCategorizer
    {
        PluginCategory GetCategory(string pluginName);
    }

    public class PluginCategorizer : IPluginCategorizer
    {
        private readonly IReadOnlyLoadOrderGraph loadOrderGraph;
        private readonly IProfilePolicy profilePolicy;

        public PluginCategorizer(IReadOnlyLoadOrderGraph loadOrderGraph, IProfilePolicy profilePolicy)
        {
            this.loadOrderGraph = loadOrderGraph;
            this.profilePolicy = profilePolicy;
        }

        public PluginCategory GetCategory(string pluginName)
        {
            if (loadOrderGraph.IsImplicit(pluginName))
                return PluginCategory.BaseGame;
            if (profilePolicy.IsLikelyOverhaul(pluginName))
                return PluginCategory.NpcOverhaul;
            if (loadOrderGraph.GetAllMasters(pluginName, true).Any(p => profilePolicy.IsLikelyOverhaul(p)))
                return PluginCategory.NpcOverhaulPatch;
            return PluginCategory.Other;
        }
    }
}
