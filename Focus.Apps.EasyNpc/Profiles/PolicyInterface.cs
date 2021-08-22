using Focus.Analysis.Execution;

namespace Focus.Apps.EasyNpc.Profiles
{
    public interface IProfilePolicy
    {
        NpcSetupAttributes GetSetupRecommendation(INpcBasicInfo npc);
        bool IsLikelyOverhaul(string pluginName);
    }

    public class NpcSetupAttributes
    {
        public string DefaultPluginName { get; private init; }
        public string FacePluginName { get; private init; }

        public NpcSetupAttributes(string defaultPluginName, string facePluginName)
        {
            DefaultPluginName = defaultPluginName;
            FacePluginName = facePluginName;
        }
    }
}
