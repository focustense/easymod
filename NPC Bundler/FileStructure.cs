using System;
using System.Collections.Generic;
using System.IO;

namespace NPC_Bundler
{
    static class FileStructure
    {
        public static readonly IReadOnlyList<string> DlcPluginNames =
            new[] { "Update.esm", "Dawnguard.esm", "HearthFires.esm", "Dragonborn.esm" };

        public static readonly string FaceMeshesPath =
            Path.Combine("meshes", "actors", "character", "facegendata", "facegeom");

        public static readonly string FaceTintsPath =
            Path.Combine("textures", "actors", "character", "facegendata", "facetint");

        private static readonly HashSet<string> dlcPluginSet = new(DlcPluginNames, StringComparer.OrdinalIgnoreCase);

        public static string GetFaceMeshFileName(string basePluginName, string localFormIdHex)
        {
            return Path.Combine(FaceMeshesPath, basePluginName, $"00{localFormIdHex}.nif");
        }

        public static string GetFaceTintFileName(string basePluginName, string localFormIdHex)
        {
            return Path.Combine(FaceTintsPath, basePluginName, $"00{localFormIdHex}.dds");
        }

        // DLCs require some special treatment as. The plugins mostly work like any other plugins, but they don't have
        // their own artifacts (BSAs or files). Instead, their artifacts are packaged into the main Skyrim archives.
        // Consistency checkers need to be aware of this so that they don't inadvertently flag the DLCs for problems,
        // especially if users create mods for cleaned masters which guides like STEP recommend.
        public static bool IsDlc(string pluginName)
        {
            return dlcPluginSet.Contains(pluginName);
        }

        public static bool IsFaceGen(string fileName)
        {
            return fileName.StartsWith(FaceMeshesPath, StringComparison.OrdinalIgnoreCase) ||
                fileName.StartsWith(FaceTintsPath, StringComparison.OrdinalIgnoreCase);
        }
    }
}