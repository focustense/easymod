using Focus.Analysis.Records;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Skyrim;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Focus.Providers.Mutagen.Analysis
{
    public interface IReferenceChecker
    {
        IEnumerable<ReferencePath> GetInvalidPaths(ISkyrimMajorRecordGetter record);
    }

    public interface IReferenceChecker<in T>
    {
        IEnumerable<ReferencePath> GetInvalidPaths(T record);
    }

    public static class ReferenceCheckerExtensions
    {
        public static IReadOnlyList<ReferencePath> SafeCheck(
            this IReferenceChecker? checker, ISkyrimMajorRecordGetter? record)
        {
            var invalidPaths = record is not null ? checker?.GetInvalidPaths(record) : null;
            return (invalidPaths ?? Enumerable.Empty<ReferencePath>()).ToList().AsReadOnly();
        }

        public static IReadOnlyList<ReferencePath> SafeCheck<T>(this IReferenceChecker<T>? checker, T? record)
        {
            var invalidPaths = record is not null ? checker?.GetInvalidPaths(record) : null;
            return (invalidPaths ?? Enumerable.Empty<ReferencePath>()).ToList().AsReadOnly();
        }
    }

    public class ReferenceChecker<T> : IReferenceChecker, IReferenceChecker<T>
        where T : class, ISkyrimMajorRecordGetter
    {
        private readonly ConcurrentDictionary<FormKey, ReferenceInfo> cache = new();
        private readonly IGroupCache groupCache;
        private readonly List<Func<T, IEnumerable<IEnumerable<ReferenceInfo>>>> invalidPathSelectors = new();

        public ReferenceChecker(IGroupCache groupCache)
        {
            this.groupCache = groupCache;
        }

        public ReferenceChecker<T> Follow<U>(
            Func<T, IFormLinkGetter<U>?> linkSelector, Action<ReferenceChecker<U>>? configure = null)
            where U : class, ISkyrimMajorRecordGetter
        {
            return Follow(r => new[] { linkSelector(r) }, configure);
        }

        public ReferenceChecker<T> Follow<U>(
            Func<T, IGenderedItemGetter<IFormLinkGetter<U>?>?> itemSelector,
            Action<ReferenceChecker<U>>? configure = null)
            where U : class, ISkyrimMajorRecordGetter
        {
            return Follow(r =>
            {
                var item = itemSelector(r);
                if (item is null)
                    return Enumerable.Empty<IFormLinkGetter<U>>();
                return new[] { item.Male, item.Female };
            }, configure);
        }

        public ReferenceChecker<T> Follow<G, U>(
            Func<T, IGenderedItemGetter<G?>?> itemSelector, Func<G, IFormLinkGetter<U>?> linkSelector,
            Action<ReferenceChecker<U>>? configure = null)
            where U : class, ISkyrimMajorRecordGetter
        {
            return Follow(itemSelector, g => new[] { linkSelector(g) }, configure);
        }

        public ReferenceChecker<T> Follow<G, U>(
            Func<T, IGenderedItemGetter<G?>?> itemSelector, Func<G, IEnumerable<IFormLinkGetter<U>?>?> linksSelector,
            Action<ReferenceChecker<U>>? configure = null)
            where U : class, ISkyrimMajorRecordGetter
        {
            return Follow(r =>
            {
                var item = itemSelector(r);
                if (item is null)
                    return Enumerable.Empty<IFormLinkGetter<U>>();
                var subItems = new[] { item.Male, item.Female }.Where(x => x is not null).Select(x => x!);
                return subItems.SelectMany(x => linksSelector(x) ?? Enumerable.Empty<IFormLinkGetter<U>>());
            }, configure);
        }

        public ReferenceChecker<T> Follow<U>(
            Func<T, IEnumerable<IFormLinkGetter<U>?>?> linksSelector, Action<ReferenceChecker<U>>? configure = null)
            where U : class, ISkyrimMajorRecordGetter
        {
            var subrecordChecker = new ReferenceChecker<U>(groupCache);
            configure?.Invoke(subrecordChecker);
            invalidPathSelectors.Add(r => CheckLinks(r, linksSelector, subrecordChecker));
            return this;
        }

        public ReferenceChecker<T> FollowSelf(Func<T, IFormLinkGetter<T>?> linkSelector)
        {
            invalidPathSelectors.Add(r => CheckLinks(r, r => new[] { linkSelector(r) }, this));
            return this;
        }

        public ReferenceChecker<T> FollowSelf(Func<T, IEnumerable<IFormLinkGetter<T>?>?> linksSelector)
        {
            invalidPathSelectors.Add(r => CheckLinks(r, linksSelector, this));
            return this;
        }

        public IEnumerable<ReferencePath> GetInvalidPaths(T record)
        {
            return invalidPathSelectors
                .Select(ps => ps(record))
                .SelectMany(refLists => refLists)
                .Select(refs => new ReferencePath(refs));
        }

        public IEnumerable<ReferencePath> GetInvalidPaths(ISkyrimMajorRecordGetter record)
        {
            if (record is not T typedRecord)
                throw new ArgumentException(
                    $"Expected a record of type {typeof(T).Name}, but got {record.GetType().Name}", nameof(record));
            return GetInvalidPaths(typedRecord);
        }

        private IEnumerable<IEnumerable<ReferenceInfo>> CheckLinks<U>(
            T record, Func<T, IEnumerable<IFormLinkGetter<U>?>?> linksSelector, ReferenceChecker<U> subrecordChecker)
            where U : class, ISkyrimMajorRecordGetter
        {
            var originKey = record.FormKey.ToRecordKey();
            var originType = typeof(T).GetRecordType();
            var originInfo = cache.GetOrAdd(
                record.FormKey, _ => new ReferenceInfo(originKey, originType, record.EditorID ?? string.Empty));
            foreach (var link in linksSelector(record) ?? Enumerable.Empty<IFormLinkGetter<U>>())
            {
                if (link is null || link.IsNull)
                    continue;
                var target = link.WinnerFrom(groupCache);
                if (target is null)
                {
                    var referenceInfo =
                        cache.GetOrAdd(link.FormKey, _ =>
                            new ReferenceInfo(link.FormKey.ToRecordKey(), link.Type.GetRecordType()));
                    yield return new[] { originInfo, referenceInfo };
                    continue;
                }
                var subrecordPaths = subrecordChecker.invalidPathSelectors
                    .Select(ps => ps(target))
                    .SelectMany(refLists => refLists)
                    .Select(path => path.Prepend(originInfo));
                foreach (var subrecordPath in subrecordPaths)
                    yield return subrecordPath;
            }
        }
    }
}
