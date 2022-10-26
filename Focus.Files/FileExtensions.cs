using System;
using System.IO;

namespace Focus.Files
{
    public static class FileExtensions
    {
        public static bool CopyToFile(
            this IArchiveProvider archiveProvider, string archivePath, string archiveFilePath, string outFilePath)
        {
            try
            {
                return TryWrite(archiveProvider.ReadBytes(archivePath, archiveFilePath), outFilePath);
            }
            catch (FileNotFoundException)
            {
                return false;
            }
        }

        public static bool CopyToFile(
            this IFileProvider fileProvider, string providerFilePath, string outFilePath)
        {
            try
            {
                return TryWrite(fileProvider.ReadBytes(providerFilePath), outFilePath);
            }
            catch (FileNotFoundException)
            {
                return false;
            }
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