using Loqui;
using Mutagen.Bethesda.Skyrim;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Focus.Analysis.Records
{
    public interface ISubAnalyzer<TResult>
    {
        Type RecordType { get; }

        TResult Analyze(ISkyrimMajorRecordGetter record);
    }

    public interface ISubAnalyzer<T, TResult> : ISubAnalyzer<TResult>
        where T : class, ISkyrimMajorRecordGetter
    {
        TResult Analyze(T record);
    }

    public interface ISubAnalyzerBundle<TResult>
    {
        TResult? Analyze(ISkyrimMajorRecordGetter record, TResult? defaultValue = default);
    }

    public abstract class SubAnalyzer<T, TResult> : ISubAnalyzer<T, TResult>
        where T : class, ISkyrimMajorRecordGetter
    {
        public Type RecordType => typeof(T);

        public abstract TResult Analyze(T record);

        public TResult Analyze(ISkyrimMajorRecordGetter record)
        {
            if (typeof(T).IsAssignableFrom(record.GetType()))
                return Analyze((T)record);
            throw new ArgumentException(
                $"Unsupported record type: {record.GetType().Name} (expected a subtype of: ${typeof(T).Name})",
                nameof(record));
        }
    }

    public class SubAnalyzerBundle<TResult> : ISubAnalyzerBundle<TResult>
    {
        private readonly Dictionary<Type, ISubAnalyzer<TResult>> subAnalyzers;

        public SubAnalyzerBundle(IEnumerable<ISubAnalyzer<TResult>> subAnalyzers)
        {
            this.subAnalyzers = subAnalyzers.ToDictionary(x => x.RecordType);
        }

        public TResult? Analyze(ISkyrimMajorRecordGetter record, TResult? defaultValue)
        {
            var getterType = LoquiRegistration.GetRegister(record.GetType()).GetterType;
            if (subAnalyzers.TryGetValue(getterType, out var analyzer))
                return analyzer.Analyze(record);
            return defaultValue ?? default;
        }
    }
}
