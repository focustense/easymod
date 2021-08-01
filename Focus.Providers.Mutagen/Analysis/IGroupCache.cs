using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Skyrim;
using Noggog;
using System;
using System.Collections.Generic;
using RecordType = Focus.Analysis.Records.RecordType;

namespace Focus.Providers.Mutagen.Analysis
{
    public interface IGroupCache
    {
        IReadOnlyCache<ISkyrimMajorRecordGetter, FormKey>? Get(string pluginName, Type groupType);
        IGroupCommonGetter<T>? Get<T>(string pluginName, Func<ISkyrimModGetter, IGroupCommonGetter<T>> groupSelector)
            where T : class, ISkyrimMajorRecordGetter;
        IEnumerable<IKeyValue<T, string>> GetAll<T>(IFormLinkGetter<T> link)
            where T : class, ISkyrimMajorRecordGetter;
        ISkyrimModGetter? GetMod(string pluginName);
        T? GetWinner<T>(IFormLinkGetter<T> link)
            where T : class, ISkyrimMajorRecordGetter;
        bool MasterExists(FormKey formKey, RecordType recordType);
    }

    public static class GroupCacheExtensions
    {
        public static IEnumerable<IKeyValue<T, string>> AllFrom<T>(this IFormLinkGetter<T> link, IGroupCache cache)
            where T : class, ISkyrimMajorRecordGetter
        {
            return cache.GetAll(link);
        }

        public static IReadOnlyCache<ISkyrimMajorRecordGetter, FormKey>? Get(
            this IGroupCache cache, string pluginName, RecordType recordType)
        {
            return cache.Get(pluginName, recordType.GetGroupType());
        }

        public static T? WinnerFrom<T>(this IFormLinkGetter<T> link, IGroupCache cache)
            where T : class, ISkyrimMajorRecordGetter
        {
            return cache.GetWinner(link);
        }
    }
}