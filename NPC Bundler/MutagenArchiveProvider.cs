using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NPC_Bundler
{
    public class MutagenArchiveProvider : IArchiveProvider
    {
        private readonly GameEnvironmentState<ISkyrimMod, ISkyrimModGetter> environment;

        public MutagenArchiveProvider(GameEnvironmentState<ISkyrimMod, ISkyrimModGetter> environment)
        {
            this.environment = environment;
        }

        public IEnumerable<string> GetArchiveFileNames(string archivePath, string path)
        {
            var reader = Archive.CreateReader(GameRelease.SkyrimSE, archivePath);
            return reader.Files
                .Where(f => string.IsNullOrEmpty(path) || f.Path.StartsWith(path, StringComparison.OrdinalIgnoreCase))
                .Select(f => f.Path);
        }

        public IEnumerable<string> GetLoadedArchivePaths()
        {
            // Currently, GetApplicableArchivePaths ignores the ModKey entirely and just reads every available BSA.
            // This is actually fine for our purposes, but the code has to be aware of this to avoid duplication.
            return Archive.GetApplicableArchivePaths(GameRelease.SkyrimSE, environment.GameFolderPath, ModKey.Null);
        }
    }
}