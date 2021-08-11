#nullable enable

using Focus.Apps.EasyNpc.Profile;
using System.Collections.Generic;
using System.Linq;

namespace Focus.Apps.EasyNpc.Profiles
{
    public class NpcRestorationFailure
    {
        public IReadOnlyList<ProfileEvent> Events { get; private init; }
        public bool IsDefaultPluginInvalid { get; private init; }
        public bool IsFaceModInvalid { get; private init; }
        public bool IsFacePluginInvalid { get; private init; }
        public NpcModel Npc { get; private init; }

        public NpcRestorationFailure(
            NpcModel npc, IEnumerable<ProfileEvent> events, bool isDefaultPluginInvalid, bool isFacePluginInvalid,
            bool isFaceModInvalid)
        {
            Npc = npc;
            Events = events.ToList().AsReadOnly();
            IsDefaultPluginInvalid = isDefaultPluginInvalid;
            IsFaceModInvalid = isFaceModInvalid;
            IsFacePluginInvalid = isFacePluginInvalid;
        }
    }
}
