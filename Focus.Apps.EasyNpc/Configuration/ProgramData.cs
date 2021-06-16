﻿using Focus.Apps.EasyNpc.Configuration;
using System;
using System.IO;

namespace Focus.Apps.EasyNpc.Configuration
{
    static class ProgramData
    {
        public static string ConfiguredMugshotsPath => !string.IsNullOrEmpty(BundlerSettings.Default.MugshotsDirectory) ?
            BundlerSettings.Default.MugshotsDirectory : DefaultMugshotsPath;

        public static readonly string DefaultMugshotsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Mugshots");
        public static readonly string DirectoryPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AssemblyProperties.Name);
        public static readonly string LogFileName =
            Path.Combine(DirectoryPath, $"Log_{DateTime.Now:yyyyMMdd_HHmmss_fffffff}.txt");
        public static readonly string ProfileLogFileName = Path.Combine(DirectoryPath, "Profile.log");
    }
}