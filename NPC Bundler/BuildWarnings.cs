﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace NPC_Bundler
{
    public enum BuildWarningId
    {
        ModDirectoryNotSpecified = 1,
        ModDirectoryNotFound,
        MultipleArchiveSources,
        FaceModNotSpecified,
        FaceModNotInstalled,
        FaceModPluginMismatch,
        FaceModMissingFaceGen,
        FaceModExtraFaceGen,
        FaceModMultipleFaceGen,
    }

    public class BuildWarning
    {
        // Used for help links, if provided.
        public BuildWarningId? Id { get; init; }
        public string Message { get; init; }
        public string PluginName { get; init; }

        public BuildWarning() { }

        public BuildWarning(string message) : this()
        {
            Message = message;
        }

        public BuildWarning(BuildWarningId id, string message) : this(message)
        {
            Id = id;
        }

        public BuildWarning(string pluginName, BuildWarningId id, string message)
            : this(id, message)
        {
            PluginName = pluginName;
        }
    }

    public class BuildWarningIdsToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is IEnumerable<BuildWarningId> warningIds ?
                string.Join(", ", warningIds.Select(id => Enum.GetName(id))) : "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Enumerable.Empty<BuildWarningId>();
        }
    }

    static class WarningMessages
    {
        private static readonly string ModRootJustification =
            "Without direct access to your mod structure, this program can only generate a merged plugin, which " +
            "will probably break NPC appearances unless you are manually organizing the facegen data.";

        public static string FaceModExtraFaceGen(string editorId, string name, string modName)
        {
            return
                $"{NpcLabel(editorId, name)} does not override vanilla face attributes, but mod ({modName}) " +
                $"contains facegen data.";
        }

        public static string FaceModMissingFaceGen(string editorId, string name, string modName)
        {
            return $"No FaceGen mesh found for {NpcLabel(editorId, name)} in mod ({modName}).";
        }

        public static string FaceModMultipleFaceGen(string editorId, string name, string modName)
        {
            return
                $"Mod ({modName}) provides a FaceGen mesh for {NpcLabel(editorId, name)} both as a loose file " +
                $"AND in an archive. The loose file will take priority.";
        }

        public static string FaceModNotInstalled(string editorId, string name, string modName)
        {
            return $"{NpcLabel(editorId, name)} uses a face mod ({modName}) that is not installed or not detected.";
        }

        public static string FaceModNotSpecified(string editorId, string name)
        {
            return
                $"{NpcLabel(editorId, name)} uses a plugin with face overrides but doesn't have any face mod " +
                $"selected.";
        }

        public static string FaceModPluginMismatch(string editorId, string name, string modName, string pluginName)
        {
            return
                $"{NpcLabel(editorId, name)} has mismatched face mod ({modName}) and face plugin ({pluginName}).";
        }

        public static string ModDirectoryNotFound(string directoryName)
        {
            return $"Mod directory {directoryName} doesn't exist. {ModRootJustification}";
        }

        public static string ModDirectoryNotSpecified()
        {
            return "No mod directory specified in settings. " + ModRootJustification;
        }

        public static string MultipleArchiveSources(string name, IEnumerable<string> providingMods)
        {
            return $"Archive '{name}' is provided by multiple mods: [{string.Join(", ", providingMods)}].";
        }

        private static string NpcLabel(string editorId, string name)
        {
            return $"{editorId} '{name}'";
        }
    }
}