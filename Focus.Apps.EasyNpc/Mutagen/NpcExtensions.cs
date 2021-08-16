using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Plugins.Order;
using Mutagen.Bethesda.Skyrim;
using System.Linq;

namespace Focus.Apps.EasyNpc.Mutagen
{
    static class NpcExtensions
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
    }
}