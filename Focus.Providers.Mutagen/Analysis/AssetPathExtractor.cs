using Focus.Analysis.Records;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Skyrim;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Focus.Providers.Mutagen.Analysis
{
    using AssetReferences = IEnumerable<AssetReference>;

    public interface IAssetPathExtractor<T>
        where T : class, ISkyrimMajorRecordGetter
    {
        AssetReferences GetReferencedAssets(T record);
    }

    public interface IAssetPathConfiguration
    {
        ISubAnalyzerBundle<AssetReferences> GetSubAnalyzers();
    }

    public interface IAssetPathConfigurationBuilder
    {
        IAssetPathConfiguration Build();
        IAssetPathConfigurationBuilder For<T>(Action<IAssetPathRecordConfigurationBuilder<T>> config)
            where T : class, ISkyrimMajorRecordGetter;
    }

    public interface IAssetPathRecordConfigurationBuilder<T>
    {
        IAssetPathRecordConfigurationBuilder<T> Add(AssetKind kind, Func<T, string?> pathSelector);
        IAssetPathRecordConfigurationBuilder<T> Add(AssetKind kind, Func<T, IEnumerable<string?>?> pathsSelector);
        IAssetPathRecordConfigurationBuilder<T> Add<TGendered>(
            AssetKind kind, Func<T, IGenderedItemGetter<TGendered?>?> genderedItemSelector,
            Func<TGendered, string?> pathSelector);
        IAssetPathRecordConfigurationBuilder<T> Add<TGendered>(
            AssetKind kind, Func<T, IGenderedItemGetter<TGendered?>?> genderedItemSelector,
            Func<TGendered, IEnumerable<string?>?> pathsSelector);
    }

    public class AssetPathExtractor<T> : ReferenceFollower<T, AssetReferences, AssetReferences>, IAssetPathExtractor<T>
        where T : class, ISkyrimMajorRecordGetter
    {
        private readonly ISubAnalyzerBundle<AssetReferences> subAnalyzerBundle;

        public AssetPathExtractor(IGroupCache groupCache, IAssetPathConfiguration configuration)
            : this(groupCache, configuration.GetSubAnalyzers())
        {
        }

        private AssetPathExtractor(
            IGroupCache groupCache, ISubAnalyzerBundle<AssetReferences> subAnalyzerBundle)
            : base(groupCache)
        {
            this.subAnalyzerBundle = subAnalyzerBundle;
        }

        public IAssetPathExtractor<T> ConfigureRoutes(Action<IReferenceFollower<T>> config)
        {
            config(this);
            return this;
        }

        public AssetReferences GetReferencedAssets(T record)
        {
            return WalkAll(record)
                .SelectMany(results => results)
                .SelectMany(refs => refs)
                .Distinct();
        }

        protected override AssetReferences Accumulate(AssetReferences? previous, AssetReferences current)
        {
            return current;
        }

        protected override ReferenceFollower<TNext, AssetReferences, AssetReferences> CreateChild<TNext>()
        {
            return new AssetPathExtractor<TNext>(GroupCache, subAnalyzerBundle);
        }

        protected override bool IsTerminal(AssetReferences current)
        {
            return true;
        }

        protected override void Link(T record, AssetReferences current)
        {
            var recordType = typeof(T).GetRecordType();
            foreach (var reference in current)
                reference.SourceRecordTypes.Add(recordType);
        }

        protected override AssetReferences Visit(T record)
        {
            return subAnalyzerBundle.Analyze(record)?.ToList() ?? Enumerable.Empty<AssetReference>();
        }

        protected override AssetReferences VisitMissing<TNext>(IFormLinkGetter<TNext> link)
        {
            return Enumerable.Empty<AssetReference>();
        }
    }

    public class AssetPathConfiguration : IAssetPathConfiguration
    {
        private readonly ISubAnalyzerBundle<AssetReferences> subAnalyzers;

        internal AssetPathConfiguration(ISubAnalyzerBundle<AssetReferences> subAnalyzers)
        {
            this.subAnalyzers = subAnalyzers;
        }

        public static IAssetPathConfigurationBuilder Builder()
        {
            return new AssetPathConfigurationBuilder();
        }

        public ISubAnalyzerBundle<AssetReferences> GetSubAnalyzers()
        {
            return subAnalyzers;
        }
    }

    class AssetPathConfigurationBuilder : IAssetPathConfigurationBuilder
    {
        private readonly List<Func<ISubAnalyzer<AssetReferences>>> subAnalyzerBuilders = new();

        public IAssetPathConfiguration Build()
        {
            var subAnalyzers = subAnalyzerBuilders.Select(builder => builder()).ToList();
            return new AssetPathConfiguration(new SubAnalyzerBundle<AssetReferences>(subAnalyzers));
        }

        public IAssetPathConfigurationBuilder For<T>(Action<IAssetPathRecordConfigurationBuilder<T>> config)
            where T : class, ISkyrimMajorRecordGetter
        {
            var builder = new AssetPathRecordConfigurationBuilder<T>();
            config(builder);
            subAnalyzerBuilders.Add(() => builder.Build());
            return this;
        }
    }

    class AssetPathRecordConfigurationBuilder<T> : IAssetPathRecordConfigurationBuilder<T>
        where T : class, ISkyrimMajorRecordGetter
    {
        private readonly List<Func<T, AssetReferences?>> selectors = new();

        public IAssetPathRecordConfigurationBuilder<T> Add(AssetKind kind, Func<T, string?> pathSelector)
        {
            return Add(kind, x => new[] { pathSelector(x) });
        }

        public IAssetPathRecordConfigurationBuilder<T> Add(AssetKind kind, Func<T, IEnumerable<string?>?> pathsSelector)
        {
            selectors.Add(x => pathsSelector(x)?.NotNullOrEmpty().Select(path => new AssetReference(path, kind)));
            return this;
        }

        public IAssetPathRecordConfigurationBuilder<T> Add<TGendered>(
            AssetKind kind, Func<T, IGenderedItemGetter<TGendered?>?> genderedItemSelector,
            Func<TGendered, string?> pathSelector)
        {
            return Add(kind, genderedItemSelector, x => new[] { pathSelector(x) });
        }

        public IAssetPathRecordConfigurationBuilder<T> Add<TGendered>(
            AssetKind kind, Func<T, IGenderedItemGetter<TGendered?>?> genderedItemSelector,
            Func<TGendered, IEnumerable<string?>?> pathsSelector)
        {
            Add(kind, x =>
            {
                var genderedItem = genderedItemSelector(x);
                if (genderedItem is null)
                    return null;
                return new[] { genderedItem.Male, genderedItem.Female }
                    .Where(item => item is not null)
                    .SelectMany(item => pathsSelector(item!) ?? Enumerable.Empty<string>());
            });
            return this;
        }

        public ISubAnalyzer<T, AssetReferences> Build()
        {
            return new AssetPathSubExtractor<T>(selectors);
        }
    }

    class AssetPathSubExtractor<T> : SubAnalyzer<T, AssetReferences>
        where T : class, ISkyrimMajorRecordGetter
    {
        private readonly IEnumerable<Func<T, IEnumerable<AssetReference?>?>> selectors;

        public AssetPathSubExtractor(IEnumerable<Func<T, IEnumerable<AssetReference?>?>> selectors)
        {
            this.selectors = selectors;
        }

        public override AssetReferences Analyze(T record)
        {
            return selectors
                .SelectMany(extract => extract(record) ?? Enumerable.Empty<AssetReference>())
                .NotNull();
        }
    }
}
