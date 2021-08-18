﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace Focus.Apps.EasyNpc.Build
{
    public enum BuildWarningId
    {
        Unknown = 0,
        ModDirectoryNotSpecified = 1,
        ModDirectoryNotFound,
        BadArchive,
        MasterPluginRemoved,
        SelectedPluginRemoved,
        MissingFaceGen,
        MultipleFaceGen,
        FaceModChangesRace,
        FaceModWigNotMatched,
        FaceModWigNotMatchedBald,
        FaceModWigConversionDisabled,
        MultipleArchiveSources,
        FaceGenOverride,
    }

    public class BuildWarning
    {
        // Used for help links, if provided.
        public BuildWarningId? Id { get; init; }
        public string Message { get; init; }
        public RecordKey RecordKey { get; init; }
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

        public BuildWarning(RecordKey key, BuildWarningId id, string message) : this(id, message)
        {
            RecordKey = key;
        }

        public BuildWarning(string pluginName, BuildWarningId id, string message)
            : this(id, message)
        {
            PluginName = pluginName;
        }

        public BuildWarning(string pluginName, RecordKey key, BuildWarningId id, string message)
            : this(pluginName, id, message)
        {
            RecordKey = key;
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

        public static string BadArchive(string path)
        {
            return $"Archive '{path}' is corrupt or unreadable.";
        }

        public static string FaceGenOverride(string editorId, string name, string modName)
        {
            return
                $"{NpcLabel(editorId, name)} is configured to use mod '{modName}' as a FaceGen override, which " +
                $"bypasses FaceGen consistency checks.";
        }

        public static string FaceModChangesRace(
            string editorId, string name, string pluginName, string defaultPluginName)
        {
            return
                $"Plugin {pluginName} changes the race of NPC {NpcLabel(editorId, name)} that is set by the default " +
                $"plugin {defaultPluginName}.";
        }

        public static string FaceModWigConversionDisabled(string editorId, string name, string pluginName, bool isBald)
        {
            var outcomeText = isBald ? "be bald" : "revert to their default hair";
            return
                $"{NpcLabel(editorId, name)} in face plugin {pluginName} uses a wig, and you have de-wiggification " +
                $"disabled. This character will {outcomeText}.";
        }

        public static string FaceModWigNotMatched(string editorId, string name, string pluginName, string modelName)
        {
            return
                $"{NpcLabel(editorId, name)} in face plugin {pluginName} uses a wig with model name '{modelName}', " +
                $"which could not be matched to any known hair type. This NPC will revert to their default hair.";
        }

        public static string FaceModWigNotMatchedBald(string editorId, string name, string pluginName, string modelName)
        {
            return
                $"{NpcLabel(editorId, name)} in face plugin {pluginName} has no hair and uses a wig with model name " +
                $"'{modelName}', which could not be matched to any known hair type. This NPC will be bald.";
        }

        public static string MasterPluginRemoved(string pluginName)
        {
            return $"NPC master plugin {pluginName} is no longer installed.";
        }

        public static string MissingFaceGen(
            string editorId, string name, string pluginName, IEnumerable<string> modNames)
        {
            return
                $"Plugin '{pluginName}' makes edits to {NpcLabel(editorId, name)} that require a FaceGen file, but " +
                $"it was not found in any of the mods: [{string.Join(", ", modNames)}]";
        }

        public static string MultipleFaceGen(string editorId, string name, IEnumerable<string> modNames)
        {
            return
                $"FaceGen file for {NpcLabel(editorId, name)} is being provided by multiple mods/components: " +
                $"[{string.Join(", ", modNames)}].";
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

        public static string SelectedPluginRemoved(string editorId, string name, string fieldName, string pluginName)
        {
            return
                $"{NpcLabel(editorId, name)} references missing or disabled {pluginName} for its {fieldName} plugin " +
                $"selection.";
        }

        private static string NpcLabel(string editorId, string name)
        {
            return $"{editorId} '{name}'";
        }
    }
}