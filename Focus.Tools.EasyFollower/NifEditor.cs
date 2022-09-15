using Focus.Files;
using Serilog;

namespace Focus.Tools.EasyFollower
{
    class NifEditor
    {
        private readonly IFileProvider fileProvider;
        private readonly ILogger log;

        public NifEditor(IFileProvider fileProvider, ILogger log)
        {
            this.fileProvider = fileProvider;
            this.log = log;
        }

        public bool HasHdtPhysics(string fileName)
        {
            const string dataName = "HDT Skinned Mesh Physics Object";

            log.Debug("Checking for HDT physics in {fileName}", fileName);
            using var nif = LoadNifFile(fileName);
            var stringData = nif.RootNode.GetExtraStringData(dataName);
            return !string.IsNullOrEmpty(stringData);
        }

        public void MergeModels(
            IEnumerable<string> modelsToMerge, string outputPath, OutputMode outputMode)
        {
            log.Debug("Merging multiple models into {fileName}", outputPath);
            if (outputMode == OutputMode.NormalWithBackup)
            {
                log.Debug("Backing up existing file");
                Files.Backup(outputPath);
            }
            var (firstModel, remainingModels) = modelsToMerge.PickFirst();
            if (firstModel == null)
            {
                log.Debug("No models to merge; aborting.");
                return;
            }
            log.Debug("Using file {0} as merge base", firstModel);
            using var outputNif = LoadNifFile(firstModel);
            log.Information("Loaded file {0} as merge base", firstModel);
            foreach (var modelFileName in remainingModels)
            {
                log.Debug("Merging shape data from {0}", modelFileName);
                using var modelNif = LoadNifFile(modelFileName);
                var rootShapes = modelNif.RootNode.GetShapes();
                foreach (var shape in rootShapes)
                {
                    log.Debug("Copying shape {shapeName}", shape.Name);
                    shape.CopyTo(outputNif.RootNode);
                }
                log.Information("Merged shape data from {0}", modelFileName);
            }
            if (outputMode != OutputMode.DryRun)
            {
                outputNif.SaveToFile(outputPath);
                log.Information("Saved {fileName}", outputPath);
            }
            else
                log.Information("Skipped save due to dry run.");
        }

        public void RemoveNodes(
            string fileName, Predicate<string> namePredicate, OutputMode outputMode)
        {
            log.Debug("Removing unwanted nodes from {fileName}", fileName);
            using var nif = LoadNifFile(fileName);
            var physicsNodes = nif.RootNode.GetChildren()
                .Where(x => namePredicate(x.Name))
                .ToList();
            foreach (var node in physicsNodes)
            {
                // Node name will disappear on delete, so we need to make a copy of it.
                var nodeName = node.Name;
                node.Delete();
                log.Information("Removed node {nodeName}", nodeName);
            }
            var faceGenShapes =
                nif.FindNode("BSFaceGenNiNodeSkinned")?
                    .GetShapes()
                    .Where(x => namePredicate(x.Name))
                ?? Enumerable.Empty<NifShape>();
            foreach (var shape in faceGenShapes)
            {
                var shapeName = shape.Name;
                shape.Delete();
                log.Information("Removed shape {shapeName}", shapeName);
            }
            if (outputMode == OutputMode.NormalWithBackup)
                Files.Backup(fileName);
            if (outputMode != OutputMode.DryRun)
            {
                nif.SaveToFile(fileName);
                log.Information("Saved {fileName}", fileName);
            }
            else
                log.Information("Skipped save due to dry run.");
        }

        private Nif LoadNifFile(string fileName)
        {
            var data = fileProvider.ReadBytes(fileName);
            return new Nif(data.ToArray());
        }
    }
}
