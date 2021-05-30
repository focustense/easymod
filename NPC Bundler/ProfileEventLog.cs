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
            using var fs = File.OpenRead(fileName);
            using var reader = new StreamReader(fs);
            string line;
            while ((line = reader.ReadLine()) != null)
                yield return ProfileEvent.Deserialize(line);
        }

        private bool disposed = false;
        private readonly StreamWriter writer;

        public ProfileEventLog(string fileName)
        {
            writer = new StreamWriter(fileName, true);
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

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                    writer.Dispose();
                disposed = true;
            }
        }
    }
}