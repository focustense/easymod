using Focus.Apps.EasyNpc.Mutagen;
using Focus.Providers.Mutagen;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Serilog;
using System.Collections.Generic;
using System.Threading.Tasks;
using NpcRecord = Mutagen.Bethesda.Skyrim.Npc;

namespace Focus.Apps.EasyNpc.Build.Pipeline
{
    public class NpcDefaultsTask : BuildTask<NpcDefaultsTask.Result>
    {
        public class Result
        {
            public IReadOnlyList<(Profiles.Npc Model, NpcRecord Record)> Npcs { get; private init; }

            public Result(IReadOnlyList<(Profiles.Npc model, NpcRecord record)> npcs)
            {
                Npcs = npcs;
            }
        }

        public delegate NpcDefaultsTask Factory(PatchInitializationTask.Result patch);

        private readonly IReadOnlyGameEnvironment<ISkyrimModGetter> env;
        private readonly ILogger log;
        private readonly PatchInitializationTask.Result patch;

        public NpcDefaultsTask(
            IReadOnlyGameEnvironment<ISkyrimModGetter> env, ILogger log, PatchInitializationTask.Result patch)
        {
            this.env = env;
            this.log = log;
            this.patch = patch;
        }

        protected override Task<Result> Run(BuildSettings settings)
        {
            return Task.Run(() =>
            {
                ItemCount.OnNext(settings.Profile.Count);
                var npcs = new List<(Profiles.Npc model, NpcRecord record)>();
                foreach (var npc in settings.Profile.Npcs)
                {
                    log.Debug("Importing default attributes for {npcLabel}", npc.DescriptiveLabel);
                    NextItem(npc.DescriptiveLabel);
                    if (npc.HasUnmodifiedFaceTemplate())
                    {
                        log.Information(
                            "Skipping {npcLabel} because all overrides use the same template.", npc.DescriptiveLabel);
                        continue;
                    }
                    var defaultModKey = ModKey.FromNameAndExtension(npc.DefaultOption.PluginName);
                    var defaultNpc = env.GetModNpc(defaultModKey, npc.ToFormKey());
                    var mergedNpcRecord = patch.Mod.Npcs.GetOrAddAsOverride(defaultNpc);
                    npcs.Add((npc, mergedNpcRecord));
                    patch.Importer.AddMaster(npc.DefaultOption.PluginName);
                }
                return new Result(npcs);
            });
        }
    }
}
