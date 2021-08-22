using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;
using Focus.Apps.EasyNpc.Build;
using Focus.Files;
using nifly;
using Serilog;

namespace Focus.Apps.EasyNpc.Nifly
{
    public class NiflyFaceGenEditor : IFaceGenEditor
    {
        private readonly IFileProvider fileProvider;
        private readonly IFileSync fileSync;
        private readonly IFileSystem fs;
        private readonly ILogger log;

        public NiflyFaceGenEditor(IFileSystem fs, IFileProvider fileProvider, IFileSync fileSync, ILogger log)
        {
            this.fileProvider = fileProvider;
            this.fileSync = fileSync;
            this.fs = fs;
            this.log = log;
        }

        public async Task<bool> ReplaceHeadParts(
            string faceGenPath, IEnumerable<HeadPartInfo> removedParts, IEnumerable<HeadPartInfo> addedParts,
            Color? hairColorNullable)
        {
            if (!File.Exists(faceGenPath))
            {
                log.Error("Couldn't find FaceGen file at {faceGenPath}", faceGenPath);
                return false;
            }

            using var _ = fileSync.Lock(faceGenPath);

            var faceGenData = await fs.File.ReadAllBytesAsync(faceGenPath);
            using var faceGenFile = new NifFile(new vectoruchar(faceGenData));
            var faceGenNode = faceGenFile.GetNodes().FirstOrDefault(x => x.name.get() == "BSFaceGenNiNodeSkinned");
            if (faceGenNode is null)
            {
                log.Error(
                    "FaceGen file '{faceGenPath}' cannot be processed because it is missing a BSFaceGenNiNodeSkinned " +
                    "node. Either it is not a valid FaceGen file or it is for the wrong game.", faceGenPath);
                return false;
            }

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
                if (string.IsNullOrEmpty(addedPart.EditorId) || string.IsNullOrEmpty(addedPart.FileName))
                {
                    log.Error(
                        "Unable to complete dewiggify on '{faceGenPath}' because the head part info is missing " +
                        "one or more required fields. [EditorId = '{editorId}', FileName = '{fileName}']",
                        faceGenPath, addedPart.EditorId, addedPart.FileName);
                    return false;
                }
                if (existingShapeNames.Contains(addedPart.EditorId))
                    continue;
                var modelData = fileProvider.ReadBytes(addedPart.FileName).ToArray();
                using var modelFile = new NifFile(new vectoruchar(modelData));
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
            // Doesn't seem to be any better way to do this (i.e. asynchronously) right now unless we change the SWIG
            // bindings at the source.
            faceGenFile.Save(faceGenPath);
            return true;
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
