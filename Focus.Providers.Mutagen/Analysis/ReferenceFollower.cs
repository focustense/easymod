using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Skyrim;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Focus.Providers.Mutagen.Analysis
{
    public interface IReferenceFollower<T>
        where T : class, ISkyrimMajorRecordGetter
    {
        IReferenceFollower<T> Follow<TNext>(
            Func<T, IFormLinkGetter<TNext>?> linkSelector, Action<IReferenceFollower<TNext>>? configure = null)
            where TNext : class, ISkyrimMajorRecordGetter;

        IReferenceFollower<T> Follow<TNext>(
            Func<T, IGenderedItemGetter<IFormLinkGetter<TNext>?>?> itemSelector,
            Action<IReferenceFollower<TNext>>? configure = null)
            where TNext : class, ISkyrimMajorRecordGetter;

        IReferenceFollower<T> Follow<TGendered, TNext>(
            Func<T, IGenderedItemGetter<TGendered?>?> itemSelector,
            Func<TGendered, IFormLinkGetter<TNext>?> linkSelector,
            Action<IReferenceFollower<TNext>>? configure = null)
            where TNext : class, ISkyrimMajorRecordGetter;

        IReferenceFollower<T> Follow<TGendered, TNext>(
            Func<T, IGenderedItemGetter<TGendered?>?> itemSelector,
            Func<TGendered, IEnumerable<IFormLinkGetter<TNext>?>?> linksSelector,
            Action<IReferenceFollower<TNext>>? configure = null)
            where TNext : class, ISkyrimMajorRecordGetter;

        IReferenceFollower<T> Follow<TNext>(
            Func<T, IEnumerable<IFormLinkGetter<TNext>?>?> linksSelector,
            Action<IReferenceFollower<TNext>>? configure = null)
            where TNext : class, ISkyrimMajorRecordGetter;

        IReferenceFollower<T> FollowSelf(Func<T, IFormLinkGetter<T>?> linkSelector);

        IReferenceFollower<T> FollowSelf(Func<T, IEnumerable<IFormLinkGetter<T>?>?> linksSelector);
    }

    public abstract class ReferenceFollower<T, TResult> : IReferenceFollower<T>
        where T : class, ISkyrimMajorRecordGetter
    {
        protected IGroupCache GroupCache { get; private init; }
        protected ConcurrentDictionary<FormKey, TResult> ResultCache { get; private init; } = new();

        private readonly List<Func<T, IEnumerable<IEnumerable<TResult>>>> routes = new();

        public ReferenceFollower(IGroupCache groupCache)
        {
            GroupCache = groupCache;
        }

        public IReferenceFollower<T> Follow<TNext>(
            Func<T, IFormLinkGetter<TNext>?> linkSelector, Action<IReferenceFollower<TNext>>? configure = null)
            where TNext : class, ISkyrimMajorRecordGetter
        {
            return Follow(r => new[] { linkSelector(r) }, configure);
        }

        public IReferenceFollower<T> Follow<TNext>(
            Func<T, IGenderedItemGetter<IFormLinkGetter<TNext>?>?> itemSelector,
            Action<IReferenceFollower<TNext>>? configure = null)
            where TNext : class, ISkyrimMajorRecordGetter
        {
            return Follow(r =>
            {
                var item = itemSelector(r);
                if (item is null)
                    return Enumerable.Empty<IFormLinkGetter<TNext>>();
                return new[] { item.Male, item.Female };
            }, configure);
        }

        public IReferenceFollower<T> Follow<TGendered, TNext>(
            Func<T, IGenderedItemGetter<TGendered?>?> itemSelector,
            Func<TGendered, IFormLinkGetter<TNext>?> linkSelector,
            Action<IReferenceFollower<TNext>>? configure = null)
            where TNext : class, ISkyrimMajorRecordGetter
        {
            return Follow(itemSelector, g => new[] { linkSelector(g) }, configure);
        }

        public IReferenceFollower<T> Follow<TGendered, TNext>(
            Func<T, IGenderedItemGetter<TGendered?>?> itemSelector,
            Func<TGendered, IEnumerable<IFormLinkGetter<TNext>?>?> linksSelector,
            Action<IReferenceFollower<TNext>>? configure = null)
            where TNext : class, ISkyrimMajorRecordGetter
        {
            return Follow(r =>
            {
                var item = itemSelector(r);
                if (item is null)
                    return Enumerable.Empty<IFormLinkGetter<TNext>>();
                var subItems = new[] { item.Male, item.Female }.Where(x => x is not null).Select(x => x!);
                return subItems.SelectMany(x => linksSelector(x) ?? Enumerable.Empty<IFormLinkGetter<TNext>>());
            }, configure);
        }

        public IReferenceFollower<T> Follow<TNext>(
            Func<T, IEnumerable<IFormLinkGetter<TNext>?>?> linksSelector,
            Action<IReferenceFollower<TNext>>? configure = null)
            where TNext : class, ISkyrimMajorRecordGetter
        {
            var subrecordFollower = CreateChild<TNext>();
            configure?.Invoke(subrecordFollower);
            routes.Add(r => Walk(r, linksSelector, subrecordFollower));
            return this;
        }

        public IReferenceFollower<T> FollowSelf(Func<T, IFormLinkGetter<T>?> linkSelector)
        {
            routes.Add(r => Walk(r, r => new[] { linkSelector(r) }, this));
            return this;
        }

        public IReferenceFollower<T> FollowSelf(Func<T, IEnumerable<IFormLinkGetter<T>?>?> linksSelector)
        {
            routes.Add(r => Walk(r, linksSelector, this));
            return this;
        }

        protected IEnumerable<IEnumerable<TResult>> WalkAll(T record)
        {
            return routes
                .Select(walk => walk(record))
                .SelectMany(resultSets => resultSets);
        }

        private IEnumerable<IEnumerable<TResult>> Walk<U>(
            T record,
            Func<T, IEnumerable<IFormLinkGetter<U>?>?> linksSelector,
            ReferenceFollower<U, TResult> subrecordFollower)
            where U : class, ISkyrimMajorRecordGetter
        {
            var originKey = record.FormKey.ToRecordKey();
            var originType = typeof(T).GetRecordType();
            var currentResult = ResultCache.GetOrAdd(record.FormKey, _ => Visit(record));
            foreach (var link in linksSelector(record) ?? Enumerable.Empty<IFormLinkGetter<U>>())
            {
                if (link is null || link.IsNull)
                    continue;
                var target = link.WinnerFrom(GroupCache);
                if (target is null)
                {
                    var targetResult = ResultCache.GetOrAdd(link.FormKey, _ => VisitMissing(link));
                    yield return new[] { currentResult, targetResult };
                    continue;
                }
                var subrecordResults = subrecordFollower.routes
                    .Select(ps => ps(target))
                    .SelectMany(refLists => refLists)
                    .Select(path => path.Prepend(currentResult));
                foreach (var subrecordPath in subrecordResults)
                    yield return subrecordPath;
            }
        }

        protected abstract ReferenceFollower<TNext, TResult> CreateChild<TNext>()
            where TNext : class, ISkyrimMajorRecordGetter;
        protected abstract TResult Visit(T record);
        protected abstract TResult VisitMissing<TNext>(IFormLinkGetter<TNext> link)
            where TNext : class, ISkyrimMajorRecordGetter;
    }
}
