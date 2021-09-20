using Focus.Files;
using PropertyChanged;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Focus.Apps.EasyNpc.Reports
{
    public record ArchiveFile(string ArchivePath, string RelativePath)
    {
        public string ArchiveName => Path.GetFileName(ArchivePath);
    }

    [AddINotifyPropertyChangedInterface]
    public class ArchiveExtractorViewModel
    {
        public delegate ArchiveExtractorViewModel Factory(IEnumerable<ArchiveFile> files, string outputDirectory);

        public int Count => files.Count;
        public ArchiveFile CurrentFile { get; private set; } = new("", "");
        public int CurrentIndex { get; private set; }
        public bool IsCompleted { get; private set; }

        private readonly IArchiveProvider archiveProvider;
        private readonly IReadOnlyList<ArchiveFile> files;
        private readonly string outputDirectory;
        private readonly object statusLock = new();

        public ArchiveExtractorViewModel(
            IArchiveProvider archiveProvider, IEnumerable<ArchiveFile> files, string outputDirectory)
        {
            this.archiveProvider = archiveProvider;
            this.files = files.ToList();
            this.outputDirectory = outputDirectory;
        }

        public void ExtractAll()
        {
            Parallel.ForEach(files, file =>
            {
                lock (statusLock)
                {
                    CurrentFile = file;
                    CurrentIndex++;
                }
                var outputPath = Path.Combine(outputDirectory, file.RelativePath);
                archiveProvider.CopyToFile(file.ArchivePath, file.RelativePath, outputPath);
            });
            IsCompleted = true;
        }
    }
}
