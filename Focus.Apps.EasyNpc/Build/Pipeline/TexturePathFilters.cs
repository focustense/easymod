using Focus.Files;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Focus.Apps.EasyNpc.Build.Pipeline
{
    // Implementation note: The Texture Copy Task already excludes face tints, so filters do not need to handle them.
    public interface ITexturePathFilter
    {
        bool ShouldImport(string texturePath);
    }

    // Copy tasks already ignore assets that are not in any mods (i.e. vanilla-only). This takes it one step further and
    // excludes any assets that are in vanilla BSAs, even if they are overridden by some other mod.
    //
    // The idea is to avoid merging "texture overrides", i.e. textures that are not really essential to NPC overhauls
    // and may not even be related to those overhauls, they just happen to be referenced by a record as vanilla texture
    // paths often are, and also overridden by some unrelated mod in the load order.
    //
    // It's not possible to divine intent with 100% certainty, but most of the time this is going to be correct, and if
    // not correct, it is easy to correct afterward by adding those textures back as loose files or in another mod,
    // since textures can never really "conflict" in the way facegens and some meshes can.
    public class VanillaTextureOverrideExclusion : ITexturePathFilter
    {
        private readonly IArchiveProvider archiveProvider;
        private readonly Lazy<IReadOnlyCollection<string>> baseGameArchivePaths;

        public VanillaTextureOverrideExclusion(IArchiveProvider archiveProvider, IGameSettings gameSettings)
        {
            this.archiveProvider = archiveProvider;

            baseGameArchivePaths = new(
                () => gameSettings.ArchiveOrder
                    .Where(f => gameSettings.IsBaseGameArchive(f))
                    .Select(f => Path.Combine(gameSettings.DataDirectory, f))
                    .ToList(),
                true);
        }

        public bool ShouldImport(string texturePath)
        {
            return !baseGameArchivePaths.Value.Any(p => archiveProvider.ContainsFile(p, texturePath));
        }
    }
}
