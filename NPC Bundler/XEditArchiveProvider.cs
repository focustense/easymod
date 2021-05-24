using System;
using System.Collections.Generic;
using XeLib.API;

namespace NPC_Bundler
{
    public class XEditArchiveProvider : IArchiveProvider
    {
        public IEnumerable<string> GetArchiveFileNames(string archivePath, string path)
        {
            return Resources.GetContainerFiles(archivePath, path);
        }

        public IEnumerable<string> GetLoadedArchivePaths()
        {
            return Resources.GetLoadedContainers();
        }
    }
}