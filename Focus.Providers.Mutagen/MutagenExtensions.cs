using Loqui;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Environments;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Plugins.Records;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Focus.Providers.Mutagen
{
    public static class MutagenExtensions
    {
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

        public static string GetRecordTypeName<T>()
        {
            return GetRecordTypeName(typeof(T));
        }

        public static string GetRecordTypeName(Type recordType)
        {
            return LoquiRegistration.TryGetRegister(recordType, out var reg) ? reg.Name : recordType.Name;
        }

        public static T PickGender<T>(this IGenderedItemGetter<T> item, bool female)
        {
            return female ? item.Female : item.Male;
        }

        public static bool SequenceEqualSafe<T>(
            this IEnumerable<T>? first, IEnumerable<T>? second, Func<T, FormKey?> keySelector)
        {
            Func<T, string> wrappedKeySelector = x => (keySelector(x) ?? FormKey.Null).ToString();
            return (first ?? Enumerable.Empty<T>()).Select(wrappedKeySelector)
                .SequenceEqual((second ?? Enumerable.Empty<T>()).Select(wrappedKeySelector));
        }

        public static bool SetEqualsSafe<T>(
            this IEnumerable<T>? first, IEnumerable<T>? second, Func<T, FormKey?> keySelector)
        {
            // FormKey instances aren't Comparable.
            // To compare sequences, we don't care about the "correct order" (i.e. load order), only that the order is
            // consistent between both sequences.
            Func<T, string> wrappedKeySelector = x => (keySelector(x) ?? FormKey.Null).ToString();
            return first.OrderBySafe(wrappedKeySelector).SequenceEqualSafe(second.OrderBySafe(wrappedKeySelector));
        }

        public static bool SetEqualsSafeBy<T, TKey>(
            this IEnumerable<T>? first, IEnumerable<T>? second, Func<T, TKey> keySelector)
        {
            return first.OrderBySafe(keySelector).SequenceEqualSafe(second.OrderBySafe(keySelector));
        }

        public static FormKey ToFormKey(this IRecordKey recordKey)
        {
            return FormKey.Factory($"{recordKey.LocalFormIdHex}:{recordKey.BasePluginName}");
        }

        public static IEnumerable<FormKey> ToFormKeys(this IEnumerable<RecordKey> recordKeys)
        {
            return recordKeys.Select(x => x.ToFormKey());
        }

        public static RecordKey ToRecordKey(this FormKey formKey)
        {
            return new RecordKey(formKey.ModKey.FileName, formKey.IDString());
        }

        public static IReadOnlyList<RecordKey> ToRecordKeys(this IEnumerable<FormKey> formKeys)
        {
            return formKeys.Select(x => x.ToRecordKey()).ToList().AsReadOnly();
        }

        public static IReadOnlyList<RecordKey> ToRecordKeys<T>(this IEnumerable<IFormLinkGetter<T>> formLinks)
            where T : class, IMajorRecordGetter
        {
            return formLinks.Select(x => x.FormKeyNullable).NotNull().Select(x => x.ToRecordKey()).ToList().AsReadOnly();
        }

        public static FormKey? ToSystemNullable(this FormKey key)
        {
            return key.IsNull ? null : key;
        }

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

        public static TMajor? TryResolve<TMajor>(
            this IFormLinkGetter<TMajor> link, ILinkCache cache, IMajorRecordGetter? source, ILogger log,
            string? consequence = null)
            where TMajor : class, IMajorRecordGetter
        {
            if (link.IsNull)
                return null;
            var record = link.TryResolve(cache);
            if (record == null)
            {
                var message = new StringBuilder();
                var messageArgs = new List<object?>();
                if (source != null)
                {
                    var sourceType = GetRecordTypeName(source.GetType());
                    message.Append("{sourceType:l} {formKey} ({editorId}) includes");
                    messageArgs.AddRange(new object?[] { sourceType, source.FormKey, source.EditorID });
                }
                else
                    message.Append("found");
                var linkType = GetRecordTypeName<TMajor>();
                message.Append(" reference to missing {linkType:l} {referencedFormKey}.");
                messageArgs.AddRange(new object[] { linkType, link.FormKey });
                if (!string.IsNullOrEmpty(consequence))
                    message.Append(' ').Append(consequence);
                log.Warning(message.ToString(), messageArgs.ToArray());
            }
            return record;
        }
    }
}
