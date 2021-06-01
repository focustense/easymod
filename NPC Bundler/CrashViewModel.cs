using System;
using System.Diagnostics;
using System.IO;

namespace NPC_Bundler
{
    public class CrashViewModel
    {
        public string LogDirectory { get; private init; }
        public string LogFileName { get; private init; }

        public CrashViewModel(string logDirectory, string logFileName)
        {
            LogDirectory = logDirectory;
            LogFileName = logFileName;
        }

        public void OpenLogDirectory()
        {
            if (!Directory.Exists(LogDirectory)) // In case user moved/deleted after the build
                return;
            var psi = new ProcessStartInfo() { FileName = LogDirectory, UseShellExecute = true };
            Process.Start(psi);
        }
    }
}