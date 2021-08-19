using Focus.Providers.Mutagen;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Skyrim;
using System.Linq;

namespace Focus.Apps.EasyNpc.Mutagen
{
    static class NpcExtensions
    {
        public static INpcGetter GetMasterNpc(this IReadOnlyGameEnvironment<ISkyrimModGetter> env, FormKey formKey)
        {
            return GetModNpc(env, formKey.ModKey, formKey);
        }

        public static INpcGetter GetModNpc(
            this IReadOnlyGameEnvironment<ISkyrimModGetter> env, ModKey modKey, FormKey formKey)
        {
            // This check should always succeed in practice. In case it fails, we have the old, "slow" way below.
            // Going through LinkCache should be faster because each group access (i.e. to Mod.Npcs) actually allocates
            // a new group - the group itself is not cached.
            if (env.LinkCache is ILinkCache<ISkyrimMod, ISkyrimModGetter> typedLinkCache)
            {
                var context = GetModNpcContext(typedLinkCache, modKey, formKey);
                if (context == null)
                    throw new MissingRecordException(formKey, "Npc", modKey.FileName);
                return context.Record;
            }

            var npc = env.LoadOrder.GetIfEnabled(modKey)?.Mod?.Npcs.TryGetValue(formKey);
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