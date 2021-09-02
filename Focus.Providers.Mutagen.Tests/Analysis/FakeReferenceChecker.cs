using Focus.Analysis.Records;
using Focus.Providers.Mutagen.Analysis;
using Mutagen.Bethesda.Skyrim;
using System.Collections.Generic;

namespace Focus.Providers.Mutagen.Tests.Analysis
{
    class FakeReferenceChecker : IReferenceChecker
    {
        public IEnumerable<ReferencePath> InvalidPaths { get; set; }

        public IReferenceChecker<T> Of<T>()
        {
            return new CheckerOf<T>(this);
        }

        public IEnumerable<ReferencePath> GetInvalidPaths(ISkyrimMajorRecordGetter record)
        {
            return InvalidPaths;
        }

        class CheckerOf<T> : IReferenceChecker<T>
        {
            private readonly FakeReferenceChecker parentChecker;

            public CheckerOf(FakeReferenceChecker parentChecker)
            {
                this.parentChecker = parentChecker;
            }

            public IEnumerable<ReferencePath> GetInvalidPaths(T record)
            {
                return parentChecker.InvalidPaths;
            }
        }
    }
}
