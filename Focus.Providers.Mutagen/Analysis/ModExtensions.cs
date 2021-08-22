using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Skyrim;
using Noggog;
using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace Focus.Providers.Mutagen.Analysis
{
    delegate IReadOnlyCache<ISkyrimMajorRecordGetter, FormKey>? TopLevelGroupGetter(ISkyrimModGetter mod);

    static class ModExtensions
    {
        private static readonly ConcurrentDictionary<Type, TopLevelGroupGetter?> topLevelGroupGetters = new();

        public static IReadOnlyCache<ISkyrimMajorRecordGetter, FormKey>? GetTopLevelGroupGetter(
            this ISkyrimModGetter mod, Type groupType)
        {
            var topLevelGroupGetter = topLevelGroupGetters.GetOrAdd(groupType, t =>
            {
                if (groupType == null)
                    return null;
                if (groupType == typeof(ICellGetter))
                {
                    var cellCache = new CellCache(mod.Cells);
                    return _ => cellCache;
                }
                var getTopLevelGroupMethod = typeof(IModGetter).GetMethod(nameof(IModGetter.GetTopLevelGroupGetter))!
                    .MakeGenericMethod(groupType);
                var modParam = Expression.Parameter(typeof(ISkyrimModGetter), "mod");
                return Expression
                    .Lambda<TopLevelGroupGetter>(
                        Expression.Call(modParam, getTopLevelGroupMethod!),
                        modParam)
                    .Compile();
            });
            return topLevelGroupGetter?.Invoke(mod);
        }
    }
}