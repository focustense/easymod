using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Focus.Apps.EasyNpc.Profile
{
    public interface IReadOnlyProfileEventLog : IEnumerable<ProfileEvent> { }

    public interface IProfileEventLog : IReadOnlyProfileEventLog
    {
        void Append(ProfileEvent e);
        void Erase();
    }

    public interface ISuspendableProfileEventLog : IProfileEventLog
    {
        void Resume();
        void Suspend();
    }

    public class ProfileEventLog : IDisposable, ISuspendableProfileEventLog
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

        private bool isDisposed;
        private bool isSuspended;
        private StreamWriter writer;

        public ProfileEventLog(string fileName)
        {
            FileName = fileName;
            OpenLogFile();
        }

        public void Append(ProfileEvent e)
        {
            if (isSuspended)
                return;
            writer.WriteLine(e.Serialize());
            // Auto-flush since this is used to recover from crashes
            writer.Flush();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Erase()
        {
            writer.Dispose();
            var backupName = Path.ChangeExtension(FileName, $".{DateTime.Now:yyyyMMdd_HHmmss_fffffff}.bak");
            File.Move(FileName, backupName);
            OpenLogFile();
        }

        public IEnumerator<ProfileEvent> GetEnumerator()
        {
            return ReadEventsFromFile(FileName).GetEnumerator();
        }

        public void Resume()
        {
            isSuspended = false;
        }

        public void Suspend()
        {
            isSuspended = true;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (disposing)
                    writer.Dispose();
                isDisposed = true;
            }
        }

        private void OpenLogFile()
        {
            var fs = File.Open(FileName, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            writer = new StreamWriter(fs);
        }
    }
}