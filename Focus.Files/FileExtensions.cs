using System;
using System.IO;

namespace Focus.Files
{
    public static class FileExtensions
    {
        public static bool CopyToFile(
            this IArchiveProvider archiveProvider, string archivePath, string archiveFilePath, string outFilePath)
        {
            return TryWrite(archiveProvider.ReadBytes(archivePath, archiveFilePath), outFilePath);
        }

        public static bool CopyToFile(
            this IFileProvider fileProvider, string providerFilePath, string outFilePath)
        {
            return TryWrite(fileProvider.ReadBytes(providerFilePath), outFilePath);
        }

        private static bool TryWrite(ReadOnlySpan<byte> data, string outFilePath)
        {
            if (data == null)
                return false;
            var directoryName = Path.GetDirectoryName(outFilePath);
            if (!string.IsNullOrEmpty(directoryName))
                Directory.CreateDirectory(directoryName);
            using var fs = File.Create(outFilePath);
            fs.Write(data);
            return true;
        }
    }
}