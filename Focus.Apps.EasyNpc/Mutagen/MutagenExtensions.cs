#nullable enable

using Focus.Apps.EasyNpc.GameData.Records;
using Loqui;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Plugins.Order;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Skyrim;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Focus.Apps.EasyNpc.Mutagen
{
    static class MutagenExtensions
    {
        public static INpcGetter GetMasterNpc(this ILoadOrder<IModListing<ISkyrimModGetter>> loadOrder, FormKey formKey)
        {
            return GetModNpc(loadOrder, formKey.ModKey, formKey);
        }

        public static INpcGetter GetModNpc(
            this ILoadOrder<IModListing<ISkyrimModGetter>> loadOrder, ModKey modKey, FormKey formKey)
        {
            var npc = loadOrder.GetIfEnabled(modKey)?.Mod?.Npcs.TryGetValue(formKey);
            if (npc == null)
                throw new MissingRecordException(formKey, "Npc", modKey.FileName);
            return npc;
        }

        public static IModContext<ISkyrimMod, ISkyrimModGetter, INpc, INpcGetter>? GetModNpcContext(
            this ILinkCache<ISkyrimMod, ISkyrimModGetter> linkCache, ModKey modKey, FormKey formKey)
        {
            return linkCache.ResolveAllContexts<INpc, INpcGetter>(formKey)
                .Where(x => x.ModKey == modKey)
                .SingleOrDefault();
        }

        public static TMajor? TryResolve<TMajor>(
            this IFormLinkGetter<TMajor> link, ILinkCache cache, IMajorRecordGetter? source, ILogger log,
            string? consequence = null)
            where TMajor : class, IMajorRecordCommonGetter
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

        public static string GetRecordTypeName<T>()
        {
            return GetRecordTypeName(typeof(T));
        }

        public static string GetRecordTypeName(Type recordType)
        {
            return LoquiRegistration.TryGetRegister(recordType, out var reg) ? reg.Name : recordType.Name;
        }
    }
}