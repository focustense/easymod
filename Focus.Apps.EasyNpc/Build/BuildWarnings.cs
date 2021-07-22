using Focus.Apps.EasyNpc.GameData.Records;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace Focus.Apps.EasyNpc.Build
{
    public enum BuildWarningId
    {
        ModDirectoryNotSpecified = 1,
        ModDirectoryNotFound,
        MasterPluginRemoved,
        SelectedPluginRemoved,
        MultipleArchiveSources,
        FaceModNotSpecified,
        FaceModNotInstalled,
        FaceModPluginMismatch,
        FaceModMissingFaceGen,
        FaceModExtraFaceGen,
        FaceModMultipleFaceGen,
        FaceModWigNotMatched,
        FaceModWigNotMatchedBald,
        FaceModWigConversionDisabled,
        FaceModChangesRace,
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

        public static string FaceModChangesRace(
            string editorId, string name, string pluginName, string defaultPluginName)
        {
            return
                $"Plugin {pluginName} changes the race of NPC {NpcLabel(editorId, name)} that is set by the default " +
                $"plugin {defaultPluginName}.";
        }

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