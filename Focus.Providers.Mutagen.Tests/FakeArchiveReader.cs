using Mutagen.Bethesda.Archives;
using Noggog;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;

namespace Focus.Providers.Mutagen.Tests
{
    class FakeArchiveReader : IArchiveReader
    {
        public IEnumerable<IArchiveFile> Files => folders.Values.SelectMany(x => x.Files);

        private readonly Dictionary<string, Folder> folders = new(StringComparer.OrdinalIgnoreCase);

        public FakeArchiveReader MarkFolderCorrupted(string folderPath)
        {
            if (folders.TryGetValue(folderPath, out var folder))
                folder.IsCorrupted = true;
            return this;
        }

        public FakeArchiveReader Put(string path, string data)
        {
            return Put(path, Encoding.UTF8.GetBytes(data));
        }

        public FakeArchiveReader Put(string path, byte[] data)
        {
            var directoryName = Path.GetDirectoryName(path);
            if (!folders.TryGetValue(directoryName, out var folder))
            {
                folder = new Folder(directoryName);
                folders.Add(directoryName, folder);
            }
            folder.Put(Path.GetFileName(path), data);
            return this;
        }

        public bool TryGetFolder(string path, [MaybeNullWhen(false)] out IArchiveFolder folder)
        {
            folder = folders.TryGetValue(path, out var folderImpl) ? folderImpl : null;
            return folder != null;
        }

        class File : IArchiveFile
        {
            public string Path => path;
            public uint Size => (uint)data.Length;

            private readonly byte[] data;
            private readonly string path;

            public File(string path, byte[] data)
            {
                this.path = path;
                this.data = data;
            }

            public Stream AsStream()
            {
                return new MemoryStream(data);
            }

            public byte[] GetBytes()
            {
                return data;
            }

            public ReadOnlyMemorySlice<byte> GetMemorySlice()
            {
                return data;
            }

            public ReadOnlySpan<byte> GetSpan()
            {
                return data;
            }
        }

        class CorruptedFile : IArchiveFile
        {
            public string Path => throw Corrupted();
            public uint Size => throw Corrupted();

            public Stream AsStream()
            {
                throw Corrupted();
            }

            public byte[] GetBytes()
            {
                throw Corrupted();
            }

            public ReadOnlyMemorySlice<byte> GetMemorySlice()
            {
                throw Corrupted();
            }

            public ReadOnlySpan<byte> GetSpan()
            {
                throw Corrupted();
            }

            private static Exception Corrupted()
            {
                throw new InvalidDataException("Test corrupted file");
            }
        }

        class Folder : IArchiveFolder
        {
            public IReadOnlyCollection<IArchiveFile> Files => IsCorrupted ?
                files.Select(x => new CorruptedFile()).ToList() : files.Values;
            public bool IsCorrupted { get; set; }
            public string Path => path;

            private readonly Dictionary<string, File> files = new(StringComparer.OrdinalIgnoreCase);
            private readonly string path;

            public Folder(string path)
            {
                this.path = path;
            }

            public void Put(string fileName, byte[] data)
            {
                files[fileName] = new File(System.IO.Path.Combine(path, fileName), data);
            }
        }
    }
}
