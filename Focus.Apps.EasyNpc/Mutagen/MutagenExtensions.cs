using Focus.Apps.EasyNpc.GameData.Records;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Plugins.Order;
using Mutagen.Bethesda.Skyrim;
using System.Linq;

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
            return loadOrder.GetIfEnabled(modKey).Mod.Npcs.TryGetValue(formKey);
        }

        public static IModContext<ISkyrimMod, ISkyrimModGetter, INpc, INpcGetter> GetModNpcContext(
            this ILinkCache<ISkyrimMod, ISkyrimModGetter> linkCache, ModKey modKey, FormKey formKey)
        {
            return linkCache.ResolveAllContexts<INpc, INpcGetter>(formKey)
                .Where(x => x.ModKey == modKey)
                .SingleOrDefault();
        }

        public static RecordKey ToRecordKey(this FormKey formKey)
        {
            return new RecordKey(formKey.ModKey.FileName, formKey.IDString());
        }
    }
}