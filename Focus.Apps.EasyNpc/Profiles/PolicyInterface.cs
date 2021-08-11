#nullable enable

using Focus.Analysis.Execution;
using Focus.Apps.EasyNpc.Profile;

namespace Focus.Apps.EasyNpc.Profiles
{
    public interface IProfilePolicyFactory
    {
        IProfilePolicy GetPolicy(LoadOrderAnalysis analysis);
    }

    public interface IProfilePolicy
    {
        NpcSetupAttributes GetSetupRecommendation(INpcBasicInfo npc);
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
