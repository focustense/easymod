using Focus.Apps.EasyNpc.GameData.Records;
using System;

namespace Focus.Apps.EasyNpc.Profile
{
    public interface IProfileRuleSet
    {
        NpcConfigurationDefaults GetConfigurationDefaults<TKey>(INpc<TKey> npc)
            where TKey : struct;
    }

    public class NpcConfigurationDefaults
    {
        public string DefaultPlugin { get; init; }
        public string FacePlugin { get; init; }
    }
}