using System;
using System.Collections.Generic;
using System.IO;
using XeLib.API;

namespace NPC_Bundler
{
    public class XEditArchiveProvider : IArchiveProvider
    {
        public void CopyToFile(string archivePath, string archiveFilePath, string outFilePath)
        {
            Resources.ExtractFile(archivePath, archiveFilePath, outFilePath);
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