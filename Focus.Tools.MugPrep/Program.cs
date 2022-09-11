using CommandLine;
using Focus.Files;
using Focus.Providers.Mutagen;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Environments;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using nifly;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;

namespace Focus.Tools.MugPrep
{
    class Options
    {
        [Option('d', "directory")]
        public string DirectoryPath { get; set; }

        [Option('c', "fix-cubemaps", Default = true)]
        public bool FixCubeMaps { get; set; }

        [Option('c', "fix-facetints", Default = true)]
        public bool FixFaceTints { get; set; }

        [Option('p', "pause")]
        public bool PauseOnStart { get; set; }

        [Option('v', "verbose")]
        public bool Verbose { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(Run);
        }

        private static void Run(Options options)
        {
            var loggingLevelSwitch =
                new LoggingLevelSwitch(options.Verbose ? LogEventLevel.Debug : LogEventLevel.Information);
            var log = new LoggerConfiguration()
                .MinimumLevel.ControlledBy(loggingLevelSwitch)
                .WriteTo.Console()
                .CreateLogger();
            if (options.PauseOnStart)
            {
                Console.WriteLine("Press any key...");
                Console.ReadKey();
                Console.WriteLine();
            }
            using var env = GameEnvironment.Typical.Skyrim(SkyrimRelease.SkyrimSE);
            var baseDirectory = !string.IsNullOrEmpty(options.DirectoryPath) ?
                options.DirectoryPath : Path.Combine(env.DataFolderPath, "data");
            var faceGenDirectory = Path.Combine(baseDirectory, "meshes", "actors", "character", "facegendata", "facegeom");
            var fs = new FileSystem();
            using var tempFileCache = new TempFileCache(fs);
            var processingOptions = new FaceGenProcessingOptions {
                FixCubeMaps = options.FixCubeMaps,
                FixFaceTints = options.FixFaceTints
            };
            var faceGenProcessor = new FaceGenProcessor(env, fs, faceGenDirectory, tempFileCache, processingOptions, log);

            var faceGenPaths = faceGenProcessor.GetPaths();
            foreach (var faceGenPath in faceGenPaths)
                faceGenProcessor.Process(faceGenPath);
        }
    }

    class FaceGenPath
    {
        public string DirectoryName { get; private init; }
        public string FileName { get; private init; }
        public string FilePath => Path.Combine(DirectoryName, FileName);
        public string FormId => Path.GetFileNameWithoutExtension(FileName);
        public string LocalFormId => FormId[2..];

        public FaceGenPath(string directoryName, string fileNameOrPath)
        {
            DirectoryName = directoryName;
            FileName = Path.GetFileName(fileNameOrPath);
        }

        public FormKey AsFormKey()
        {
            return FormKey.Factory($"{LocalFormId}:{DirectoryName}");
        }
    }

    class FaceGenProcessingOptions
    {
        public bool FixCubeMaps { get; init; }
        public bool FixFaceTints { get; init; }
    }

    enum TextureSlot
    {
        Diffuse = 0,
        Normal = 1,
        Emissive = 2,
        Height = 3, // Or parallax
        Environment = 4, // Or cubemap
        EnvironmentMask = 5, // Or reflection
        InnerLayerDiffuse = 6, // Or tint
        Specular = 7,
    }

    class FaceGenProcessor
    {
        private static readonly string DefaultEyeCubeMapPath = @"textures\cubemaps\eyecubemap.dds";

        private readonly IGameEnvironment<ISkyrimMod, ISkyrimModGetter> env;
        private IReadOnlySet<string> eyeNodeNames;
        private readonly string faceGenDirectory;
        private readonly IFileProvider fileProvider;
        private readonly IFileSystem fs;
        private IReadOnlySet<string> faceNodeNames;
        private readonly FaceGenProcessingOptions options;
        private readonly Dictionary<string, SkeletonResolver> skeletonResolvers = new(StringComparer.OrdinalIgnoreCase);
        private readonly TempFileCache tempFileCache;

        public FaceGenProcessor(
            IGameEnvironment<ISkyrimMod, ISkyrimModGetter> env, IFileSystem fs, string faceGenDirectory,
            TempFileCache tempFileCache, FaceGenProcessingOptions options, ILogger log)
        {
            this.env = env;
            this.faceGenDirectory = faceGenDirectory;
            this.fs = fs;
            this.options = options;
            this.tempFileCache = tempFileCache;
            var gameSelection = new GameSelection(GameRelease.SkyrimSE);
            var archiveProvider = new MutagenArchiveProvider(gameSelection, log);
            var gameSettings = GameSettings.From(GameEnvironmentWrapper.Wrap(env), new(GameRelease.SkyrimSE));
            fileProvider = new GameFileProvider(fs, gameSettings, archiveProvider);
        }

        public IEnumerable<FaceGenPath> GetPaths()
        {
            return Directory.GetDirectories(faceGenDirectory)
                .SelectMany(dir => Directory.GetFiles(dir, "*.nif")
                    .Select(f => new FaceGenPath(Path.GetRelativePath(faceGenDirectory, dir), f)));
        }

        public void Process(FaceGenPath faceGenPath)
        {
            EnsureNodeNames();
            var npc = faceGenPath.AsFormKey().ToLink<INpcGetter>().Resolve(env.LinkCache);
            if (npc.Race.IsNull)
            {
                Console.Error.WriteLine(
                    $"NPC {faceGenPath.DirectoryName}/{faceGenPath.LocalFormId} is missing race attribute.");
                return;
            }
            var race = npc.Race.Resolve(env.LinkCache);
            var skeletalModel = npc.Configuration.Flags.HasFlag(NpcConfiguration.Flag.Female) ?
                race.SkeletalModel.Female : race.SkeletalModel.Male;
            var skeletonResolver = GetSkeletonResolver(skeletalModel.File);
            var absolutePath = Path.Combine(faceGenDirectory, faceGenPath.FilePath);
            using var file = new NifFile();
            file.Load(absolutePath);
            NiNode faceGenNode = null;
            foreach (var node in file.GetNodes())
            {
                var nodeName = node.name.get();
                if (nodeName == "BSFaceGenNiNodeSkinned")
                    faceGenNode = node;
                var worldspaceTransform = skeletonResolver.GetWorldspaceTransform(nodeName);
                if (worldspaceTransform == null)
                    continue;
                node.transform = worldspaceTransform;
            }
            if (faceGenNode != null)
            {
                if (options.FixCubeMaps)
                    FixCubeMaps(file, faceGenNode);
                if (options.FixFaceTints)
                    FixFaceTints(file, faceGenNode, faceGenPath.DirectoryName, faceGenPath.FormId);
            }
            file.Save(absolutePath);
            Console.WriteLine($"Processed {faceGenPath.FilePath}");
        }

        private void EnsureNodeNames()
        {
            if (eyeNodeNames != null)
                return;
            eyeNodeNames = GetHeadPartNodeNames(HeadPart.TypeEnum.Eyes);
            faceNodeNames = GetHeadPartNodeNames(HeadPart.TypeEnum.Face);
        }

        private void FixCubeMaps(NifFile file, NiNode faceGenNode)
        {
            var eyeShapes = GetChildShapes(file, faceGenNode)
                .Where(x => x.vertexDesc.HasFlag(VertexFlags.VF_EYEDATA) || eyeNodeNames.Contains(x.name.get()));
            foreach (var eyeShape in eyeShapes)
                ReplaceTexturePath(file, eyeShape, TextureSlot.Environment, DefaultEyeCubeMapPath, IsNullOrMissing);
        }

        private void FixFaceTints(NifFile file, NiNode faceGenNode, string modName, string formId)
        {
            var faceShapes = GetChildShapes(file, faceGenNode).Where(x => faceNodeNames.Contains(x.name.get()));
            foreach (var faceShape in faceShapes)
            {
                var defaultTintPath = $@"textures\actors\character\facegendata\facetint\{modName}\{formId}.dds";
                ReplaceTexturePath(file, faceShape, TextureSlot.InnerLayerDiffuse, defaultTintPath, IsNullOrMissing);
            }
        }

        private bool IsNullOrMissing(string filePath)
        {
            return string.IsNullOrEmpty(filePath) || !fileProvider.Exists(filePath);
        }

        private static bool ReplaceTexturePath(
            NifFile file, BSDynamicTriShape shape, TextureSlot slot, string newPath, Predicate<string> oldPathCondition)
        {
            var shader = shape.HasShaderProperty() ?
                file.GetHeader().GetBlockById(shape.ShaderPropertyRef().index) as BSLightingShaderProperty : null;
            if (shader == null)
                return false;
            var textureSet = shader.HasTextureSet() ?
                file.GetHeader().GetBlockById(shader.TextureSetRef().index) as BSShaderTextureSet : null;
            if (textureSet == null)
                return false;
            var textures = textureSet.textures.items();
            var slotIndex = (int)slot;
            var oldTexture = textures[slotIndex].get();
            if (!string.IsNullOrEmpty(oldTexture) &&
                !oldTexture.StartsWith("textures", StringComparison.OrdinalIgnoreCase))
                oldTexture = Path.Combine("textures", oldTexture);
            if (oldPathCondition == null || oldPathCondition(oldTexture))
            {
                textures[slotIndex] = new NiString(newPath);
                textureSet.textures.SetItems(textures);
                return true;
            }
            return false;
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

        private IReadOnlySet<string> GetHeadPartNodeNames(HeadPart.TypeEnum type)
        {
            return env.LoadOrder.PriorityOrder.HeadPart().WinningOverrides()
                .Where(x => x.Type == type)
                .Select(x => x.EditorID)
                .ToHashSet();
        }

        private SkeletonResolver GetSkeletonResolver(string skeletonFileName)
        {
            if (!skeletonResolvers.TryGetValue(skeletonFileName, out var resolver))
            {
                var transforms = GetWorldspaceTransforms(skeletonFileName);
                resolver = new SkeletonResolver(transforms);
                skeletonResolvers.Add(skeletonFileName, resolver);
            }
            return resolver;
        }

        private Dictionary<string, MatTransform> GetWorldspaceTransforms(string skeletonFileName)
        {
            var path = tempFileCache.GetTempPath(fileProvider, Path.Combine("meshes", skeletonFileName));
            using var file = new NifFile();
            file.Load(path);
            var result = new Dictionary<string, MatTransform>();
            var composedTransforms = new Dictionary<string, MatTransform>();
            foreach (var node in file.GetNodes())
            {
                var nodeName = node.name.get();
                if (!nodeName.StartsWith("NPC "))
                    continue;
                var worldspaceTransform = node.transform;
                var parentNode = file.GetParentNode(node);
                if (parentNode != null)
                {
                    var parentName = parentNode.name.get();
                    if (!composedTransforms.TryGetValue(parentName, out var parentTransform))
                        parentTransform = parentNode.transform;
                    worldspaceTransform = parentTransform.ComposeTransforms(node.transform);
                }
                composedTransforms.Add(nodeName, worldspaceTransform);
                result.Add(nodeName, worldspaceTransform);
            }
            return result;
        }
    }

    class SkeletonResolver
    {
        private readonly Dictionary<string, MatTransform> worldspaceTransforms;

        public SkeletonResolver(Dictionary<string, MatTransform> worldspaceTransforms)
        {
            this.worldspaceTransforms = worldspaceTransforms;
        }

        public MatTransform GetWorldspaceTransform(string nodeName)
        {
            return worldspaceTransforms.TryGetValue(nodeName, out var transform) ? transform : null;
        }
    }
}
