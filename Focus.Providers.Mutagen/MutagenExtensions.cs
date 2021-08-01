using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Records;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Focus.Providers.Mutagen
{
    public static class MutagenExtensions
    {
        public static TModGetter? TryGetMod<TModSetter, TModGetter>(
            this GameEnvironmentState<TModSetter, TModGetter> env, string pluginName, ILogger log)
            where TModSetter : class, IContextMod<TModSetter, TModGetter>, TModGetter
            where TModGetter : class, IContextGetterMod<TModSetter, TModGetter>
        {
            if (!ModKey.TryFromNameAndExtension(pluginName, out var modKey))
            {
                log.Error("Invalid plugin name: {pluginName}", pluginName);
                return null;
            }
            if (!env.LoadOrder.TryGetIfEnabledAndExists(modKey, out var mod))
            {
                log.Error("Missing or disabled plugin: {pluginName}", pluginName);
                return null;
            }
            return mod;
        }

        public static TModGetter? TryGetMod<TModGetter>(
            this IReadOnlyGameEnvironment<TModGetter> env, string pluginName, ILogger log)
            where TModGetter : class, IModGetter
        {
            if (!ModKey.TryFromNameAndExtension(pluginName, out var modKey))
            {
                log.Error("Invalid plugin name: {pluginName}", pluginName);
                return null;
            }
            if (!env.LoadOrder.TryGetIfEnabledAndExists(modKey, out var mod))
            {
                log.Error("Missing or disabled plugin: {pluginName}", pluginName);
                return null;
            }
            return mod;
        }

        public static string GetRealDataDirectory<TModGetter>(this IReadOnlyGameEnvironment<TModGetter> env)
            where TModGetter : class, IModGetter
        {
            var leafDirectory = new DirectoryInfo(env.DataFolderPath).Name;
            return leafDirectory.Equals("data", StringComparison.OrdinalIgnoreCase) ?
                env.DataFolderPath : Path.Combine(env.DataFolderPath, "data");
        }

        public static string GetRealDataDirectory<TModSetter, TModGetter>(
            this GameEnvironmentState<TModSetter, TModGetter> env)
            where TModSetter : class, IContextMod<TModSetter, TModGetter>, TModGetter
            where TModGetter : class, IContextGetterMod<TModSetter, TModGetter>
        {
            var leafDirectory = new DirectoryInfo(env.DataFolderPath).Name;
            return leafDirectory.Equals("data", StringComparison.OrdinalIgnoreCase) ?
                env.DataFolderPath : Path.Combine(env.DataFolderPath, "data");
        }

        public static bool SequenceEqualSafe<T>(
            this IEnumerable<T>? first, IEnumerable<T>? second, Func<T, FormKey?> keySelector)
        {
            // FormKey instances aren't Comparable.
            // To compare sequences, we don't care about the "correct order" (i.e. load order), only that the order is
            // consistent between both sequences.
            Func<T, string> wrappedKeySelector = x => (keySelector(x) ?? FormKey.Null).ToString();
            return first.OrderBySafe(wrappedKeySelector).SequenceEqualSafe(second.OrderBySafe(wrappedKeySelector));
        }

        public static bool SequenceEqualSafeBy<T, TKey>(
            this IEnumerable<T>? first, IEnumerable<T>? second, Func<T, TKey> keySelector)
        {
            return first.OrderBySafe(keySelector).SequenceEqualSafe(second.OrderBySafe(keySelector));
        }

        public static FormKey ToFormKey(this IRecordKey recordKey)
        {
            return FormKey.Factory($"{recordKey.LocalFormIdHex}:{recordKey.BasePluginName}");
        }

        public static RecordKey ToRecordKey(this FormKey formKey)
        {
            return new RecordKey(formKey.ModKey.FileName, formKey.IDString());
        }

        public static IReadOnlyList<RecordKey> ToRecordKeys<T>(this IReadOnlyList<IFormLinkGetter<T>> formLinks)
            where T : class, IMajorRecordCommonGetter
        {
            return formLinks.Select(x => x.FormKeyNullable).NotNull().Select(x => x.ToRecordKey()).ToList().AsReadOnly();
        }
    }
}
