using Focus.Apps.EasyNpc.Mutagen;
using Focus.Providers.Mutagen;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Skyrim;
using Serilog;
using System.Threading.Tasks;

namespace Focus.Apps.EasyNpc.Build.Pipeline
{
    public class NpcFacesTask : BuildTask<NpcFacesTask.Result>
    {
        public class Result { }

        public delegate NpcFacesTask Factory(PatchInitializationTask.Result patch, NpcDefaultsTask.Result defaults);

        private readonly NpcDefaultsTask.Result defaults;
        private readonly IReadOnlyGameEnvironment<ISkyrimModGetter> env;
        private readonly ILogger log;
        private readonly PatchInitializationTask.Result patch;

        public NpcFacesTask(
            IReadOnlyGameEnvironment<ISkyrimModGetter> env, ILogger log, PatchInitializationTask.Result patch,
            NpcDefaultsTask.Result defaults)
        {
            this.defaults = defaults;
            this.env = env;
            this.log = log;
            this.patch = patch;
        }

        protected override Task<Result> Run(BuildSettings settings)
        {
            return Task.Run(() =>
            {
                ItemCount.OnNext(defaults.Npcs.Count);
                foreach (var (unresolvedModel, record) in defaults.Npcs)
                {
                    log.Debug("Applying visual attributes for {npcLabel}", unresolvedModel.DescriptiveLabel);
                    NextItem(unresolvedModel.DescriptiveLabel);
                    if (!settings.Profile.TryResolveTemplate(unresolvedModel, out var model))
                    {
                        log.Warning(
                            "Unable to find the template {targetKey} for NPC {npcLabel} in the current profile. " +
                            "Traits cannot be copied and this character may be bugged in game.",
                            unresolvedModel.DefaultOption.Analysis.TemplateInfo?.Key, unresolvedModel.DescriptiveLabel);
                        continue;
                    }
                    if (model != unresolvedModel)
                        log.Information(
                            "Redirected NPC {npcLabel} to template {templateNpcLabel}",
                            unresolvedModel.DescriptiveLabel, model.DescriptiveLabel);
                    var faceModKey = ModKey.FromNameAndExtension(model.FaceOption.PluginName);
                    var faceMod = env.LoadOrder.GetIfEnabled(faceModKey).Mod;
                    var faceNpcRecord = env.GetModNpc(faceModKey, model.ToFormKey());
                    log.Debug("Importing shallow overrides from {pluginName}", model.FaceOption.PluginName);
                    // "Deep copy" doesn't copy dependencies, so we only do this for non-referential attributes.
                    record.DeepCopyIn(faceNpcRecord, new Npc.TranslationMask(defaultOn: false)
                    {
                        FaceMorph = true,
                        FaceParts = true,
                        TextureLighting = true,
                        TintLayers = true,
                        // Height and weight might not be entirely safe to copy without carrying over body type (WNAM),
                        // but serious problems should be extremely rare. Regardless of whether or not we copy the
                        // height/weight, we'll still end up with an NPC whose body is a hybrid of the modded NPC and
                        // the default body.
                        Height = true,
                        Weight = true,
                    });
                    // We will respect the "Opposite gender animations" flag from the overhaul mod. If an overhaul
                    // decides to make an NPC look more feminine (or masculine), then it probably wants the animations
                    // to reflect that, and this would be consistent with both their intent and the intent of the user.
                    if (faceNpcRecord.Configuration.Flags.HasFlag(NpcConfiguration.Flag.OppositeGenderAnims))
                        record.Configuration.Flags |= NpcConfiguration.Flag.OppositeGenderAnims;
                    else
                        record.Configuration.Flags &= ~NpcConfiguration.Flag.OppositeGenderAnims;
                    log.Debug("Importing head parts from {pluginName}", model.FaceOption.PluginName);
                    record.HeadParts.Clear();
                    // We don't use head parts from the record anymore; instead, use the "full list" provided by the
                    // analysis engine at startup. This ensures that the facegen can't be broken by race edits, etc.
                    foreach (var headPartKey in model.FaceOption.Analysis.MainHeadParts)
                    {
                        var sourceHeadPart = headPartKey.ToFormKey().ToLinkGetter<IHeadPartGetter>();
                        var mergedHeadPart = patch.Importer.Import(sourceHeadPart, x => x.HeadParts);
                        if (mergedHeadPart.HasValue)
                            record.HeadParts.Add(mergedHeadPart.Value);
                    }
                    foreach (var headPart in record.HeadParts)
                        patch.Importer.AddHeadPartRace(headPart, record.Race);
                    log.Debug("Importing hair color from {pluginName}", model.FaceOption.PluginName);
                    record.HairColor.SetTo(patch.Importer.Import(faceNpcRecord.HairColor, x => x.Colors));
                    log.Debug("Importing face texture from {pluginName}", model.FaceOption.PluginName);
                    record.HeadTexture.SetTo(patch.Importer.Import(faceNpcRecord.HeadTexture, x => x.TextureSets));
                    log.Debug("Importing worn armor from {pluginName}", model.FaceOption.PluginName);
                    // Like head parts, we want to use the "effective" skin here, in case it was changed by a race edit.
                    var skinKey = model.FaceOption.Analysis.SkinKey?.ToFormKey() ?? FormKey.Null;
                    record.WornArmor.SetTo(patch.Importer.Import(skinKey.ToLinkGetter<IArmorGetter>(), x => x.Armors));
                    patch.Importer.AddArmorRace(record.WornArmor, faceNpcRecord.Race, record.Race);
                }
                return new Result();
            });
        }
    }
}
