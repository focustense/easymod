﻿using System;
using System.IO;

namespace NPC_Bundler
{
    static class ProgramData
    {
        public static string AppName = "SSE NPC Bundler";
        public static string DirectoryPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppName);

        public static string GetLogFileName()
        {
            return Path.Combine(DirectoryPath, $"Log_{DateTime.Now:yyyyMMdd_HHmmss_fffffff}.txt");
        }
    }
}