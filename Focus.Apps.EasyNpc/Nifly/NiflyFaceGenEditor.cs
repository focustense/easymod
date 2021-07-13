using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Focus.Apps.EasyNpc.Build;
using Focus.Apps.EasyNpc.GameData.Files;
using Focus.Files;
using nifly;
using Serilog;

namespace Focus.Apps.EasyNpc.Nifly
{
    public class NiflyFaceGenEditor : IFaceGenEditor
    {
        private readonly IFileProvider fileProvider;
        private readonly ILogger log;

        public NiflyFaceGenEditor(IFileProvider fileProvider, ILogger log)
        {
            this.fileProvider = fileProvider;
            this.log = log.ForContext<NiflyFaceGenEditor>();
        }

        public void ReplaceHeadParts(
            string faceGenPath, IEnumerable<HeadPartInfo> removedParts, IEnumerable<HeadPartInfo> addedParts,
            Color? hairColorNullable, TempFileCache tempFileCache)
        {
            if (!File.Exists(faceGenPath))
            {
                log.Error($"[FaceGenEdit] Couldn't find FaceGen file at {faceGenPath}");
                return;
            }

            using var faceGenFile = new NifFile(true);
            faceGenFile.Load(faceGenPath);
            var faceGenNode = faceGenFile.GetNodes().FirstOrDefault(x => x.name.get() == "BSFaceGenNiNodeSkinned");

            var removedEditorIds = removedParts.Select(x => x.EditorId).ToHashSet();
            var shapesToRemove = GetChildShapes(faceGenFile, faceGenNode)
                .Where(x => removedEditorIds.Contains(x.name.get()))
                .ToList();
            foreach (var shape in shapesToRemove)
                faceGenFile.DeleteShape(shape);

            // In case we end up re-processing a facegen (i.e. due to user running merge on an old directory), we should
            // ignore head parts that are already present in the facegen with the correct name.
            var existingShapeNames = GetChildShapes(faceGenFile, faceGenNode)
                .Select(x => x.name.get())
                .ToHashSet();
            foreach (var addedPart in addedParts)
            {
                if (existingShapeNames.Contains(addedPart.EditorId))
                    continue;
                var modelPath = tempFileCache.GetTempPath(fileProvider, addedPart.FileName);
                using var modelFile = new NifFile(true);
                modelFile.Load(modelPath);
                var mainShape = GetChildShapes(modelFile, modelFile.GetRootNode()).FirstOrDefault();
                if (mainShape == null)
                    continue;
                var headPartClone = faceGenFile.CloneShape(mainShape, addedPart.EditorId, modelFile);
                faceGenFile.SetParentNode(headPartClone, faceGenNode);
                if (hairColorNullable is Color hairColor && headPartClone.HasShaderProperty() &&
                    faceGenFile.GetHeader().GetBlockById(headPartClone.ShaderPropertyRef().index)
                        is BSLightingShaderProperty shader &&
                    shader.bslspShaderType == 6 /* hair tint */)
                {
                    shader.hairTintColor = new Vector3(hairColor.R / 255f, hairColor.G / 255f, hairColor.B / 255f);
                }
            }
            faceGenFile.Save(faceGenPath);
        }

        private void CopyToTempFile(string fileName)
        {
            var data = fileProvider.ReadBytes(fileName);
            if (data == null)
            {
                log.Error("Missing asset file {FileName} for FaceGen injection", fileName);
                return;
            }
            var tempFilePath = Path.GetTempFileName();

        }

        private static IEnumerable<BSDynamicTriShape> GetChildShapes(NifFile file, NiNode parent)
        {
            var header = file.GetHeader();
            var refs = new setNiRef();
            parent.GetChildRefs(refs);
            return refs
                .Select(x => header.GetBlockById(x.index))
                .Where(x => x is BSDynamicTriShape)
                .Cast<BSDynamicTriShape>();
        }
    }
}
