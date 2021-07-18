﻿using CommandLine;
using Focus.Files;
using Focus.Providers.Mutagen;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using nifly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Focus.Tools.MugPrep
{
    class Options
    {
        [Option('d', "directory")]
        public string DirectoryPath { get; set; }

        [Option('c', "fix-cubemaps", Default = true)]
        public bool FixCubeMaps { get; set; }

        [Option('p', "pause")]
        public bool PauseOnStart { get; set; }
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
            using var tempFileCache = new TempFileCache();
            var processingOptions = new FaceGenProcessingOptions { FixCubeMaps = options.FixCubeMaps };
            var faceGenProcessor = new FaceGenProcessor(env, faceGenDirectory, tempFileCache, processingOptions);

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
    }

    class FaceGenProcessor
    {
        private static readonly string DefaultEyeCubeMapPath = @"textures\cubemaps\eyecubemap.dds";

        private readonly GameEnvironmentState<ISkyrimMod, ISkyrimModGetter> env;
        private readonly string faceGenDirectory;
        private readonly IFileProvider fileProvider;
        private readonly FaceGenProcessingOptions options;
        private readonly Dictionary<string, SkeletonResolver> skeletonResolvers = new(StringComparer.OrdinalIgnoreCase);
        private readonly TempFileCache tempFileCache;

        public FaceGenProcessor(
            GameEnvironmentState<ISkyrimMod, ISkyrimModGetter> env, string faceGenDirectory,
            TempFileCache tempFileCache, FaceGenProcessingOptions options)
        {
            this.env = env;
            this.faceGenDirectory = faceGenDirectory;
            this.options = options;
            this.tempFileCache = tempFileCache;
            var archiveProvider = new MutagenArchiveProvider(env);
            fileProvider = new GameFileProvider(env.GetRealDataDirectory(), archiveProvider);
        }

        public IEnumerable<FaceGenPath> GetPaths()
        {
            return Directory.GetDirectories(faceGenDirectory)
                .SelectMany(dir => Directory.GetFiles(dir, "*.nif")
                    .Select(f => new FaceGenPath(Path.GetRelativePath(faceGenDirectory, dir), f)));
        }

        public void Process(FaceGenPath faceGenPath)
        {
            var npc = faceGenPath.AsFormKey().AsLink<INpcGetter>().Resolve(env.LinkCache);
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
            }
            file.Save(absolutePath);
            Console.WriteLine($"Processed {faceGenPath.FilePath}");
        }

        private void FixCubeMaps(NifFile file, NiNode faceGenNode)
        {
            var childShapes = GetChildShapes(file, faceGenNode);
            var eyeShapes = childShapes.Where(x => x.vertexDesc.HasFlag(VertexFlags.VF_EYEDATA));
            foreach (var eyeShape in eyeShapes)
            {
                var shader = eyeShape.HasShaderProperty() ?
                    file.GetHeader().GetBlockById(eyeShape.ShaderPropertyRef().index) as BSLightingShaderProperty :
                    null;
                if (shader == null)
                    continue;
                var textureSet = shader.HasTextureSet() ?
                    file.GetHeader().GetBlockById(shader.TextureSetRef().index) as BSShaderTextureSet : null;
                if (textureSet == null)
                    continue;
                var textures = textureSet.textures.items();
                var cubeMapTexture = textures[4].get();
                if (!string.IsNullOrEmpty(cubeMapTexture) &&
                    !cubeMapTexture.StartsWith("textures", StringComparison.OrdinalIgnoreCase))
                    cubeMapTexture = Path.Combine("textures", cubeMapTexture);
                if (string.IsNullOrEmpty(cubeMapTexture) || !fileProvider.Exists(cubeMapTexture))
                {
                    textures[4] = new NiString(DefaultEyeCubeMapPath);
                    textureSet.textures.SetItems(textures);
                }
            }
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
