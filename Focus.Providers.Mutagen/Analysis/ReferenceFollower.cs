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

    public abstract class ReferenceFollower<T, TAccumulate, TResult> : IReferenceFollower<T>
        where T : class, ISkyrimMajorRecordGetter
    {
        protected ConcurrentDictionary<FormKey, TAccumulate> AccumulatorCache { get; private set; } = new();
        protected IGroupCache GroupCache { get; private init; }

        private readonly List<Func<T, TResult?, IEnumerable<TResult>>> routes = new();

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
            subrecordFollower.AccumulatorCache = AccumulatorCache;
            configure?.Invoke(subrecordFollower);
            routes.Add((record, previous) => Walk(record, previous, linksSelector, subrecordFollower));
            return this;
        }

        public IReferenceFollower<T> FollowSelf(Func<T, IFormLinkGetter<T>?> linkSelector)
        {
            routes.Add((record, previous) => Walk(record, previous, r => new[] { linkSelector(r) }, this));
            return this;
        }

        public IReferenceFollower<T> FollowSelf(Func<T, IEnumerable<IFormLinkGetter<T>?>?> linksSelector)
        {
            routes.Add((record, previous) => Walk(record, previous, linksSelector, this));
            return this;
        }

        protected IEnumerable<IEnumerable<TResult>> WalkAll(T record)
        {
            return routes.Select(walk => walk(record, default));
        }

        private IEnumerable<TResult> Walk<U>(
            T record,
            TResult? previous,
            Func<T, IEnumerable<IFormLinkGetter<U>?>?> linksSelector,
            ReferenceFollower<U, TAccumulate, TResult> subrecordFollower)
            where U : class, ISkyrimMajorRecordGetter
        {
            var originKey = record.FormKey.ToRecordKey();
            var originType = typeof(T).GetRecordType();
            var current = AccumulatorCache.GetOrAdd(record.FormKey, _ => Visit(record));
            var result = Accumulate(previous, current);
            if (IsTerminal(current))
                yield return result;
            foreach (var link in linksSelector(record) ?? Enumerable.Empty<IFormLinkGetter<U>>())
            {
                if (link is null || link.IsNull)
                    continue;
                var nextRecord = link.WinnerFrom(GroupCache);
                if (nextRecord is null)
                {
                    var target = AccumulatorCache.GetOrAdd(link.FormKey, _ => VisitMissing(link));
                    if (IsTerminal(target))
                        yield return Accumulate(result, target);
                    continue;
                }
                var subrecordResults = subrecordFollower.routes
                    .Select(ps => ps(nextRecord, result))
                    .SelectMany(results => results);
                foreach (var subrecordPath in subrecordResults)
                    yield return subrecordPath;
            }
        }

        protected abstract ReferenceFollower<TNext, TAccumulate, TResult> CreateChild<TNext>()
            where TNext : class, ISkyrimMajorRecordGetter;
        protected abstract TResult Accumulate(TResult? previous, TAccumulate current);
        protected abstract bool IsTerminal(TAccumulate current);
        protected abstract TAccumulate Visit(T record);
        protected abstract TAccumulate VisitMissing<TNext>(IFormLinkGetter<TNext> link)
            where TNext : class, ISkyrimMajorRecordGetter;
    }
}
