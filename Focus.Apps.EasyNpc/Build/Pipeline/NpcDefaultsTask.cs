using Focus.Apps.EasyNpc.Mutagen;
using Focus.Apps.EasyNpc.Profiles;
using Focus.Providers.Mutagen;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Focus.Apps.EasyNpc.Build.Pipeline
{
    public class NpcDefaultsTask : BuildTask<NpcDefaultsTask.Result>
    {
        public class Result
        {
            public IReadOnlyList<(NpcModel Model, Npc Record)> Npcs { get; private init; }

            public Result(IReadOnlyList<(NpcModel model, Npc record)> npcs)
            {
                Npcs = npcs;
            }
        }

        public delegate NpcDefaultsTask Factory(PatchInitializationTask.Result patch);

        public override string Name => "Import NPC Defaults";

        private readonly IReadOnlyGameEnvironment<ISkyrimModGetter> env;
        private readonly PatchInitializationTask.Result patch;

        public NpcDefaultsTask(IReadOnlyGameEnvironment<ISkyrimModGetter> env, PatchInitializationTask.Result patch)
        {
            this.env = env;
            this.patch = patch;
        }

        protected override Task<Result> Run(BuildSettings settings)
        {
            return Task.Run(() =>
            {
                ItemCount.OnNext(settings.Profile.Count);
                var npcs = new List<(NpcModel model, Npc record)>();
                foreach (var npc in settings.Profile.Npcs)
                {
                    NextItem(npc.DescriptiveLabel);
                    var defaultModKey = ModKey.FromNameAndExtension(npc.DefaultOption.PluginName);
                    var defaultNpc = env.LoadOrder.GetModNpc(defaultModKey, npc.ToFormKey());
                    var mergedNpcRecord = patch.Mod.Npcs.GetOrAddAsOverride(defaultNpc);
                    npcs.Add((npc, mergedNpcRecord));
                    patch.Importer.AddMaster(npc.DefaultOption.PluginName);
                }
                return new Result(npcs);
            });
        }
    }
}
