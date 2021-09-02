using Focus.Analysis.Records;
using Mutagen.Bethesda.Skyrim;
using Serilog;
using System.Collections.Generic;
using Xunit;

namespace Focus.Providers.Mutagen.Tests.Analysis
{
    public abstract class CommonAnalyzerFacts<TAnalyzer, TMajorRecord, TAnalysis>
        where TAnalyzer : IRecordAnalyzer<TAnalysis>
        where TAnalysis : RecordAnalysis
        where TMajorRecord : class, ISkyrimMajorRecord
    {
        private static readonly IEnumerable<ReferencePath> DummyInvalidPaths = new[]
        {
            new ReferencePath(new[]
            {
                new ReferenceInfo(new RecordKey("dummy.esp", "123456"), RecordType.Armor),
                new ReferenceInfo(new RecordKey("dummy.esp", "123457"), RecordType.ArmorAddon),
                new ReferenceInfo(new RecordKey("dummy.esp", "123458"), RecordType.TextureSet),
            }),
            new ReferencePath(new[]
            {
                new ReferenceInfo(new RecordKey("dummy.esp", "123456"), RecordType.Armor),
                new ReferenceInfo(new RecordKey("dummy.esp", "123457"), RecordType.ArmorAddon),
                new ReferenceInfo(new RecordKey("dummy.esp", "123459"), RecordType.TextureSet),
            })
        };

        protected TAnalyzer Analyzer { get; set; }
        private protected FakeGroupCache Groups { get; private set; }
        protected ILogger Logger { get; private set; }
        private protected FakeReferenceChecker ReferenceChecker { get; private set; }

        public CommonAnalyzerFacts()
        {
            // Logger is not necessarily used in any common facts, but will be used by many different analyzer types and
            // should always be constructed the same way.
            Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Debug()
                .CreateLogger();
            Groups = new FakeGroupCache();
            ReferenceChecker = new FakeReferenceChecker { InvalidPaths = DummyInvalidPaths };
        }

        [Fact]
        public void WhenRecordNotFound_ReturnsDummyInfo()
        {
            var analysis = Analyzer.Analyze("plugin.esp", new RecordKey("plugin.esp", "000001"));

            Assert.Equal("plugin.esp", analysis.BasePluginName);
            Assert.Equal("000001", analysis.LocalFormIdHex);
            Assert.Equal(Analyzer.RecordType, analysis.Type);
            Assert.False(analysis.Exists);
            Assert.Equal(string.Empty, analysis.EditorId);
            Assert.False(analysis.IsInjectedOrInvalid);
            Assert.False(analysis.IsOverride);
            Assert.Empty(analysis.InvalidPaths);
        }

        [Fact]
        public void WhenRecordIsWrongType_ReturnsDummyInfo()
        {
            // It's important that we actually test the generic versions of these methods, but just in case we actually
            // add an analyzer for our dummy "other" type, we need to check the actual type.
            var addedKeys = typeof(TMajorRecord) == typeof(Explosion) ?
                Groups.AddRecords<SoulGem>("plugin.esp", x => { }) :
                Groups.AddRecords<Explosion>("plugin.esp", x => { });
            var analysis = Analyzer.Analyze("plugin.esp", addedKeys[0]);

            Assert.Equal("plugin.esp", analysis.BasePluginName);
            Assert.Equal(addedKeys[0].LocalFormIdHex, analysis.LocalFormIdHex);
            Assert.Equal(Analyzer.RecordType, analysis.Type);
            Assert.False(analysis.Exists);
            Assert.Equal(string.Empty, analysis.EditorId);
            Assert.False(analysis.IsInjectedOrInvalid);
            Assert.False(analysis.IsOverride);
            Assert.Empty(analysis.InvalidPaths);
        }

        [Fact]
        public void WhenRecordFound_ReturnsRecordInfo()
        {
            var addedKeys = Groups.AddRecords<TMajorRecord>(
                "plugin.esp",
                x => x.EditorID = "record1",
                x => x.EditorID = "record2");
            var analysis = Analyzer.Analyze("plugin.esp", addedKeys[1]);

            Assert.Equal("plugin.esp", analysis.BasePluginName);
            Assert.Equal(addedKeys[1].LocalFormIdHex, analysis.LocalFormIdHex);
            Assert.Equal(Analyzer.RecordType, analysis.Type);
            Assert.True(analysis.Exists);
            Assert.Equal("record2", analysis.EditorId);
            Assert.False(analysis.IsInjectedOrInvalid);
            Assert.False(analysis.IsOverride);
            Assert.Equal(DummyInvalidPaths, analysis.InvalidPaths);
        }

        [Fact]
        public void WhenRecordKeyIsOtherPlugin_IsOverride_IsTrue()
        {
            var addedKeys = Groups.AddRecords<TMajorRecord>("plugin.esp", "master.esp", x => { });
            var analysis = Analyzer.Analyze("plugin.esp", addedKeys[0]);

            Assert.True(analysis.IsOverride);
        }

        [Fact]
        public void WhenMasterHasRecord_IsInjectedOrInvalid_IsFalse()
        {
            var masterKeys = Groups.AddRecords<TMajorRecord>("master.esp", x => { });
            Groups.AddRecords<TMajorRecord>("plugin.esp", "master.esp", x => { });
            var analysis = Analyzer.Analyze("plugin.esp", masterKeys[0]);

            Assert.False(analysis.IsInjectedOrInvalid);
        }

        [Fact]
        public void WhenMasterMissingRecord_IsInjectedOrInvalid_IsTrue()
        {
            var addedKeys = Groups.AddRecords<TMajorRecord>("plugin.esp", "master.esp", x => { });
            var analysis = Analyzer.Analyze("plugin.esp", addedKeys[0]);

            Assert.True(analysis.IsInjectedOrInvalid);
        }
    }
}
