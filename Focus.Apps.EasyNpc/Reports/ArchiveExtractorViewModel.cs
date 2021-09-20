using Focus.Files;
using Focus.ModManagers;
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
        public delegate ArchiveExtractorViewModel Factory(IEnumerable<ArchiveFile> files, ModComponentInfo component);

        public string ComponentName => component.Name;
        public int Count => files.Count;
        public ArchiveFile CurrentFile { get; private set; } = new("", "");
        public int CurrentIndex { get; private set; }
        public bool IsCompleted { get; private set; }
        public bool IsStarted { get; private set; }

        private readonly IArchiveProvider archiveProvider;
        private readonly IReadOnlyList<ArchiveFile> files;
        private readonly ModComponentInfo component;
        private readonly object statusLock = new();

        public ArchiveExtractorViewModel(
            IArchiveProvider archiveProvider, IEnumerable<ArchiveFile> files, ModComponentInfo component)
        {
            this.archiveProvider = archiveProvider;
            this.component = component;
            this.files = files.ToList();
        }

        public void ExtractAll()
        {
            if (IsStarted)
                return;
            IsStarted = true;
            Task.Run(() =>
            {
                Parallel.ForEach(files, file =>
                {
                    lock (statusLock)
                    {
                        CurrentFile = file;
                        CurrentIndex++;
                    }
                    var outputPath = Path.Combine(component.Path, file.RelativePath);
                    archiveProvider.CopyToFile(file.ArchivePath, file.RelativePath, outputPath);
                });
                IsCompleted = true;
            });
        }
    }
}
