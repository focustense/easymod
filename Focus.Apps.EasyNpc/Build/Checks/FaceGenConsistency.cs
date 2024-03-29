﻿using Focus.Apps.EasyNpc.GameData.Files;
using Focus.Apps.EasyNpc.Profiles;
using Focus.ModManagers;
using System.Collections.Generic;
using System.Linq;

namespace Focus.Apps.EasyNpc.Build.Checks
{
    public class FaceGenConsistency : INpcBuildCheck
    {
        private readonly IModRepository modRepository;

        public FaceGenConsistency(IModRepository modRepository)
        {
            this.modRepository = modRepository;
        }

        public IEnumerable<BuildWarning> Run(INpc npc, BuildSettings _)
        {
            if (!npc.CanCustomizeFace)
                yield break;
            if (npc.FaceGenOverride is not null)
                // Eventually, we'll want to open up the override file and actually check the nodes. This check is
                // temporarily out of scope, which means reporting a generic warning.
                yield return new BuildWarning(
                    new RecordKey(npc),
                    BuildWarningId.FaceGenOverride,
                    WarningMessages.FaceGenOverride(npc.EditorId, npc.Name, npc.FaceGenOverride.Name));

            // Waste of time to check anything pointing to vanilla (except with FaceGen overrides, as above)
            if (npc.FaceOption.IsBaseGame)
                yield break;

            var defaultHeadParts = npc.DefaultOption.Analysis.MainHeadParts;
            var faceHeadParts = npc.FaceOption.Analysis.MainHeadParts;
            var requiresFaceGen = !faceHeadParts.ToHashSet().SetEquals(defaultHeadParts);
            if (!requiresFaceGen)
                yield break;
            var pluginComponents = modRepository
                .SearchForFiles(npc.FaceOption.PluginName, false)
                .Select(x => x.ModComponent)
                .Distinct();
            var faceGenPath = FileStructure.GetFaceMeshFileName(npc);
            var faceGenComponents = pluginComponents
                .Where(c => modRepository.ContainsFile(new[] { c }, faceGenPath, true))
                .ToList();
            if (faceGenComponents.Count == 0)
                yield return new BuildWarning(
                    npc.FaceOption.PluginName,
                    new RecordKey(npc),
                    BuildWarningId.MissingFaceGen,
                    WarningMessages.MissingFaceGen(
                        npc.EditorId, npc.Name, npc.FaceOption.PluginName, pluginComponents.Select(x => x.Name)));
            else if (faceGenComponents.Count > 1)
                yield return new BuildWarning(
                    npc.FaceOption.PluginName,
                    new RecordKey(npc),
                    BuildWarningId.MultipleFaceGen,
                    WarningMessages.MultipleFaceGen(npc.EditorId, npc.Name, faceGenComponents.Select(x => x.Name)));
        }
    }
}
