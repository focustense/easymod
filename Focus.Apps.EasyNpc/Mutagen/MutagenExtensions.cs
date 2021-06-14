using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using System.Linq;

namespace Focus.Apps.EasyNpc.Mutagen
{
    static class MutagenExtensions
    {
        public static INpcGetter GetMasterNpc(this LoadOrder<IModListing<ISkyrimModGetter>> loadOrder, FormKey formKey)
        {
            return GetModNpc(loadOrder, formKey.ModKey, formKey);
        }

        public static INpcGetter GetModNpc(
            this LoadOrder<IModListing<ISkyrimModGetter>> loadOrder, ModKey modKey, FormKey formKey)
        {
            return loadOrder.GetIfEnabled(modKey).Mod.Npcs.TryGetValue(formKey);
        }

        public static IModContext<ISkyrimMod, ISkyrimModGetter, INpc, INpcGetter> GetModNpcContext(
            this ILinkCache<ISkyrimMod, ISkyrimModGetter> linkCache, ModKey modKey, FormKey formKey)
        {
            return linkCache.ResolveAllContexts<INpc, INpcGetter>(formKey)
                .Where(x => x.ModKey == modKey)
                .SingleOrDefault();
        }
    }
}