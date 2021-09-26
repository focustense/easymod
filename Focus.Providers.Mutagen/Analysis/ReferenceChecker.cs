using Focus.Analysis.Records;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Focus.Providers.Mutagen.Analysis
{
    public interface IReferenceChecker
    {
        IEnumerable<ReferencePath> GetInvalidPaths(ISkyrimMajorRecordGetter record);
    }

    public interface IReferenceChecker<T>
        where T : class, ISkyrimMajorRecordGetter
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
            where T : class, ISkyrimMajorRecordGetter
        {
            var invalidPaths = record is not null ? checker?.GetInvalidPaths(record) : null;
            return (invalidPaths ?? Enumerable.Empty<ReferencePath>()).ToList().AsReadOnly();
        }
    }

    public class ReferenceChecker<T> :
        ReferenceFollower<T, ReferenceInfo, IEnumerable<ReferenceInfo>>, IReferenceChecker<T>
        where T : class, ISkyrimMajorRecordGetter
    {
        public ReferenceChecker(IGroupCache groupCache)
            : base(groupCache)
        {
        }

        public IReferenceChecker<T> Configure(Action<IReferenceFollower<T>> config)
        {
            config(this);
            return this;
        }

        public IEnumerable<ReferencePath> GetInvalidPaths(T record)
        {
            return WalkAll(record)
                .SelectMany(paths => paths)
                .Select(refs => new ReferencePath(refs));
        }

        public IEnumerable<ReferencePath> GetInvalidPaths(ISkyrimMajorRecordGetter record)
        {
            if (record is not T typedRecord)
                throw new ArgumentException(
                    $"Expected a record of type {typeof(T).Name}, but got {record.GetType().Name}", nameof(record));
            return GetInvalidPaths(typedRecord);
        }

        protected override IEnumerable<ReferenceInfo> Accumulate(IEnumerable<ReferenceInfo>? previous, ReferenceInfo current)
        {
            return (previous ?? Enumerable.Empty<ReferenceInfo>()).Append(current);
        }

        protected override ReferenceFollower<TNext, ReferenceInfo, IEnumerable<ReferenceInfo>> CreateChild<TNext>()
        {
            return new ReferenceChecker<TNext>(GroupCache);
        }

        protected override bool IsTerminal(ReferenceInfo current)
        {
            return !current.Exists;
        }

        protected override ReferenceInfo Visit(T record)
        {
            return new ReferenceInfo(
                record.FormKey.ToRecordKey(), typeof(T).GetRecordType(), record.EditorID ?? string.Empty);
        }

        protected override ReferenceInfo VisitMissing<TNext>(IFormLinkGetter<TNext> link)
        {
            return new ReferenceInfo(link.FormKey.ToRecordKey(), link.Type.GetRecordType()) { Exists = false };
        }
    }
}
