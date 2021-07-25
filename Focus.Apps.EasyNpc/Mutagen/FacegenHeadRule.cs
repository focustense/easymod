#nullable enable

using Focus.Apps.EasyNpc.Compatibility;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Serilog.Events;

namespace Focus.Apps.EasyNpc.Mutagen
{
    public class FacegenHeadRule : ICompatibilityRule<INpcGetter>
    {
        public string Description => "NPC race must have a moddable face, i.e. use the FaceGen system. Mostly humans.";
        public string Name => "FaceGen Head Required";
        public LogEventLevel LogLevel => LogEventLevel.Debug;

        private readonly GameEnvironmentState<ISkyrimMod, ISkyrimModGetter> env;

        public FacegenHeadRule(GameEnvironmentState<ISkyrimMod, ISkyrimModGetter> env)
        {
            this.env = env;
        }

        public bool IsSupported(INpcGetter gameRecord)
        {
            var race = gameRecord.Race.TryResolve(env.LinkCache);
            return race?.Flags.HasFlag(Race.Flag.FaceGenHead) ?? false;
        }
    }
}