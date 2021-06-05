using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using XeLib.API;

namespace NPC_Bundler
{
    public class XEditArchiveProvider : IArchiveProvider
    {
        public bool ContainsFile(string archivePath, string archiveFilePath)
        {
            var filesInPath = Resources.GetContainerFiles(archivePath, Path.GetDirectoryName(archiveFilePath));
            return filesInPath.Contains(archiveFilePath, StringComparer.OrdinalIgnoreCase);
        }

        public void CopyToFile(string archivePath, string archiveFilePath, string outFilePath)
        {
            Resources.ExtractFile(archivePath, archiveFilePath, outFilePath);
        }

        public IGameFileProvider CreateGameFileProvider()
        {
            return new VirtualGameFileProvider(Meta.GetGlobal("DataPath"), this);
        }

        public IEnumerable<string> GetArchiveFileNames(string archivePath, string path)
        {
            return Resources.GetContainerFiles(archivePath, path);
        }

        public IEnumerable<string> GetLoadedArchivePaths()
        {
            return Resources.GetLoadedContainers();
        }

        public string ResolvePath(string archiveName)
        {
            return Path.Combine(Meta.GetGlobal("DataPath"), archiveName);
        }
    }
}