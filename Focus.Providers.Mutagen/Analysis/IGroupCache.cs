using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Skyrim;
using Noggog;
using System;
using System.Collections.Generic;
using System.Linq;
using RecordType = Focus.Analysis.Records.RecordType;

namespace Focus.Providers.Mutagen.Analysis
{
    public interface IGroupCache
    {
        IReadOnlyCache<ISkyrimMajorRecordGetter, FormKey>? Get(string pluginName, Type groupType);
        IGroupGetter<T>? Get<T>(string pluginName, Func<ISkyrimModGetter, IGroupGetter<T>> groupSelector)
            where T : class, ISkyrimMajorRecordGetter;
        IEnumerable<IKeyValue<string, T>> GetAll<T>(IFormLinkGetter<T> link)
            where T : class, ISkyrimMajorRecordGetter;
        ISkyrimModGetter? GetMod(string pluginName);
        T? GetWinner<T>(IFormLinkGetter<T> link)
            where T : class, ISkyrimMajorRecordGetter;
        IKeyValue<string, T>? GetWinnerWithSource<T>(IFormLinkGetter<T> link)
            where T : class, ISkyrimMajorRecordGetter;
        bool MasterExists(FormKey formKey, RecordType recordType);
        void Purge();
    }

    public static class GroupCacheExtensions
    {
        public static IEnumerable<IKeyValue<string, T>> AllFrom<T>(this IFormLinkGetter<T> link, IGroupCache cache)
            where T : class, ISkyrimMajorRecordGetter
        {
            return cache.GetAll(link);
        }

        public static IReadOnlyCache<ISkyrimMajorRecordGetter, FormKey>? Get(
            this IGroupCache cache, string pluginName, RecordType recordType)
        {
            return cache.Get(pluginName, recordType.GetGroupType());
        }

        public static ContextRelations<T> GetRelations<T>(this IGroupCache groups, IFormLink<T> link, string pluginName)
            where T : class, ISkyrimMajorRecordGetter
        {
            var mod = groups.GetMod(pluginName);
            var masterNames = mod?.ModHeader.MasterReferences
                .Select(x => x.Master.FileName.String)
                .ToHashSet(StringComparer.CurrentCultureIgnoreCase)
                ?? new HashSet<string>();
            var previousContexts = link.AllFrom(groups)
                .SkipWhile(x => !string.Equals(x.Key, pluginName, StringComparison.CurrentCultureIgnoreCase))
                .Skip(1)
                .ToList();
            return new()
            {
                // Contexts are in priority order, i.e. reverse of listing order. We want the opposite.
                Base = previousContexts.Count > 0 ? previousContexts[^1] : null,
                Previous = previousContexts.Count > 0 ? previousContexts[0] : null,
                Masters = ReverseOf(previousContexts).Where(x => masterNames.Contains(x.Key)),
            };
        }

        public static T? MasterFrom<T>(this IFormLinkGetter<T> link, IGroupCache cache)
            where T : class, ISkyrimMajorRecordGetter
        {
            return cache.GetAll(link).Select(x => x.Value).LastOrDefault();
        }

        public static T? WinnerFrom<T>(this IFormLinkGetter<T> link, IGroupCache cache)
            where T : class, ISkyrimMajorRecordGetter
        {
            return cache.GetWinner(link);
        }

        private static IEnumerable<T> ReverseOf<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i >= 0; i--)
                yield return list[i];
        }
    }

    public class ContextRelations<T>
        where T : ISkyrimMajorRecordGetter
    {
        public IKeyValue<string, T>? Base { get; init; } = null;
        public IEnumerable<IKeyValue<string, T>> Masters { get; init; } = Enumerable.Empty<IKeyValue<string, T>>();
        public IKeyValue<string, T>? Previous { get; init; } = null;
    }
}