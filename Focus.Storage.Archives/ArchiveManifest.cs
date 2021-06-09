using System;
using System.Collections.Generic;
using System.Text;

namespace Focus.Storage.Archives
{
    class ArchiveManifest : IDisposable
    {
        public uint Count => Functions.BsaEntryListCount(entryList);

        public string this[uint index]
        {
            get
            {
                const int maxLength = 255;
                var sb = new StringBuilder(maxLength);
                Functions.BsaEntryListGet(entryList, index, maxLength, sb);
                return sb.ToString();
            }
        }

        internal IntPtr Handle => entryList;

        private readonly IntPtr entryList = Functions.BsaEntryListCreate();

        private bool disposed;

        public ArchiveManifest()
        {
        }

        ~ArchiveManifest()
        {
            Dispose(false);
        }

        public ArchiveManifest Add(string pathInArchive)
        {
            Functions.BsaEntryListAdd(entryList, pathInArchive);
            return this;
        }

        public ArchiveManifest AddAll(IEnumerable<string> pathsInArchive)
        {
            foreach (var path in pathsInArchive)
                Add(path);
            return this;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;
            if (disposing)
                Functions.BsaEntryListFree(entryList);
            disposed = true;
        }
    }
}