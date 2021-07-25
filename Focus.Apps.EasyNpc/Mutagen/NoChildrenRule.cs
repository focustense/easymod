#nullable enable

using Focus.Apps.EasyNpc.Compatibility;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Serilog.Events;

namespace Focus.Apps.EasyNpc.Mutagen
{
    public class NoChildrenRule : ICompatibilityRule<INpcGetter>
    {
        public string Description =>
            "Child overhauls such as RS Children and The Kids Are Alright make changes to races and headparts as " +
            "well as the NPCs themselves. The app is not currently able to handle these correctly, so modding of " +
            "child actors is currently disabled until this is sorted out, so that EasyNPC will not overwrite any " +
            "of their edits.";
        public string Name => "No Children";
        public LogEventLevel LogLevel => LogEventLevel.Warning;

        private readonly GameEnvironmentState<ISkyrimMod, ISkyrimModGetter> env;

        public NoChildrenRule(GameEnvironmentState<ISkyrimMod, ISkyrimModGetter> env)
        {
            this.env = env;
        }

        public bool IsSupported(INpcGetter gameRecord)
        {
            var race = gameRecord.Race.TryResolve(env.LinkCache);
            return !race?.Flags.HasFlag(Race.Flag.Child) ?? true;
        }
    }
}