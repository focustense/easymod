using Focus.Files;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Focus.Apps.EasyNpc.Build.Pipeline
{
    public interface IFileCopier
    {
        void CopyAll(
            HashSet<string> paths, string outputDirectory, Action<string> beforeCopy,
            out IImmutableList<string> failedPaths, CancellationToken cancellationToken);
    }

    public class FileCopier : IFileCopier
    {
        private readonly IFileProvider fileProvider;

        public FileCopier(IFileProvider fileProvider)
        {
            this.fileProvider = fileProvider;
        }

        public void CopyAll(
            HashSet<string> paths, string outputDirectory, Action<string> beforeCopy,
            out IImmutableList<string> failedPaths, CancellationToken cancellationToken)
        {
            var mutableFailedPaths = new List<string>();
            // Problem: we don't know for certain whether a file is loose, or coming from a BSA. Loose files are I/O
            // bound, but BSAs are a combination of I/O and CPU.
            // Probably the best we can do is use a few (but not too many) concurrent tasks here, and hope that the
            // benefit of occasionally optimizing some BSA work outweighs the cost of multiple synchronous I/O ops.
            // The latter should be a non-issue on SSDs but could be significant on a mechanical HDD.
            Parallel.ForEach(paths, new ParallelOptions { MaxDegreeOfParallelism = 4 }, path =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                beforeCopy(path);
                var outputPath = Path.Combine(outputDirectory, path);
                if (!fileProvider.CopyToFile(path, outputPath))
                    lock (mutableFailedPaths)
                        mutableFailedPaths.Add(outputPath);
            });
            failedPaths = mutableFailedPaths.ToImmutableList();
            paths.ExceptWith(failedPaths);
        }
    }
}
