using System;
using System.Collections.Generic;
using System.IO;

namespace NPC_Bundler
{
    public class ProfileEventLog : IDisposable
    {
        public static IEnumerable<ProfileEvent> ReadEventsFromFile(string fileName)
        {
            if (!File.Exists(fileName))
                yield break;
            using var fs = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(fs);
            string line;
            while ((line = reader.ReadLine()) != null)
                yield return ProfileEvent.Deserialize(line);
        }

        public string FileName { get; private init; }

        private bool disposed = false;
        private StreamWriter writer;

        public ProfileEventLog(string fileName)
        {
            FileName = fileName;
            OpenLogFile();
        }

        ~ProfileEventLog()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Append(ProfileEvent e)
        {
            writer.WriteLine(e.Serialize());
            // Auto-flush since this is used to recover from crashes
            writer.Flush();
        }

        public void Erase()
        {
            writer.Dispose();
            var backupName = Path.ChangeExtension(FileName, $".{DateTime.Now:yyyyMMdd_HHmmss_fffffff}.bak");
            File.Move(FileName, backupName);
            OpenLogFile();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                    writer.Dispose();
                disposed = true;
            }
        }

        private void OpenLogFile()
        {
            var fs = File.Open(FileName, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            writer = new StreamWriter(fs);
        }
    }
}