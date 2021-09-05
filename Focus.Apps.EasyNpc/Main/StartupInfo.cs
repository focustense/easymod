using Focus.Apps.EasyNpc.Configuration;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Focus.Apps.EasyNpc.Main
{
    public enum ModManager
    {
        None = -1,
        Unknown = 0,
        ModOrganizer = 1,
        Vortex = 2
    }

    public class StartupInfo
    {
        public static bool IsLaunchedByModOrganizer => instance?.Launcher == ModManager.ModOrganizer;
        public static bool IsLaunchedByUnknown => instance?.Launcher == ModManager.Unknown;
        public static bool IsLaunchedByVortex => instance?.Launcher == ModManager.Vortex;
        public static bool IsLaunchedStandalone => instance?.Launcher == ModManager.None;
        public static bool IsLaunchedStandaloneOrUnknown => IsLaunchedByUnknown || IsLaunchedStandalone;

        private static StartupInfo instance;

        public ModManager Launcher { get; private init; }
        public ModManager ModDirectoryOwner { get; private init; }
        public string ParentProcessPath { get; private init; }

        public static StartupInfo Detect()
        {
            var parentProcess = Process.GetCurrentProcess().Parent();
            instance = new StartupInfo
            {
                Launcher = DetectProcessModManager(parentProcess),
                ModDirectoryOwner = DetectModDirectoryModManager(Settings.Default.DefaultModRootDirectory),
                ParentProcessPath = parentProcess?.MainModule?.FileName,
            };
            return instance;
        }

        private StartupInfo() { }

        private static ModManager DetectModDirectoryModManager(string directoryPath)
        {
            if (string.IsNullOrEmpty(directoryPath) || !Directory.Exists(directoryPath))
                return ModManager.None;
            if (File.Exists(Path.Combine(directoryPath, "__vortex_staging_folder")))
                return ModManager.Vortex;
            // For Mod Organizer, there's nothing special about the mod directory itself. However, one unique thing
            // about MO is that it adds a metadata file to each subdirectory containing details about the mod, since it
            // does not maintain its own central "database".
            // It is technically possible for a user to have deleted these files, although they really shouldn't. We can
            // work around one or two accidental deletions by checking a few different mods, if available.
            // We also don't want to check too many directories, in case of something odd like a mod directory being
            // copied *from* Mod Organizer when most directories don't match.
            if (Directory.EnumerateDirectories(directoryPath).Take(3)
                .Any(dir => File.Exists(Path.Combine(dir, "meta.ini"))))
                return ModManager.ModOrganizer;
            return ModManager.Unknown;
        }

        private static ModManager DetectProcessModManager(Process process)
        {
            if (process == null)
                return ModManager.None;
            if (process.ProcessName.Equals("modorganizer", StringComparison.OrdinalIgnoreCase))
                return ModManager.ModOrganizer;
            if (process.ProcessName.Equals("vortex", StringComparison.OrdinalIgnoreCase))
                return ModManager.Vortex;
            return ModManager.Unknown;
        }
    }
}