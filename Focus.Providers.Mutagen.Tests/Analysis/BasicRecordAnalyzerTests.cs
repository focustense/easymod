using Focus.Providers.Mutagen.Analysis;
using Moq;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Noggog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using RecordType = Focus.Analysis.Records.RecordType;

namespace Focus.Providers.Mutagen.Tests.Analysis
{
    public class BasicRecordAnalyzerTests
    {
        private readonly BasicRecordAnalyzer analyzer;
        private readonly FakeGroupCache groups;
        private readonly RecordType recordType = RecordType.Container;

        public BasicRecordAnalyzerTests()
        {
            groups = new FakeGroupCache();
            analyzer = new BasicRecordAnalyzer(groups, recordType);
        }

        [Fact]
        public void WhenRecordNotFound_ReturnsDummyInfo()
        {
            var analysis = analyzer.Analyze("plugin.esp", new RecordKey("plugin.esp", "000001"));

            Assert.Equal("plugin.esp", analysis.BasePluginName);
            Assert.Equal("000001", analysis.LocalFormIdHex);
            Assert.Equal(recordType, analysis.Type);
            Assert.False(analysis.Exists);
            Assert.Equal(string.Empty, analysis.EditorId);
            Assert.False(analysis.IsInjectedOrInvalid);
            Assert.False(analysis.IsOverride);
        }

        [Fact]
        public void WhenRecordIsWrongType_ReturnsDummyInfo()
        {
            groups.AddRecords<Explosion>("plugin.esp", x => { });
            var analysis = analyzer.Analyze("plugin.esp", new RecordKey("plugin.esp", "000001"));

            Assert.Equal("plugin.esp", analysis.BasePluginName);
            Assert.Equal("000001", analysis.LocalFormIdHex);
            Assert.Equal(recordType, analysis.Type);
            Assert.False(analysis.Exists);
            Assert.Equal(string.Empty, analysis.EditorId);
            Assert.False(analysis.IsInjectedOrInvalid);
            Assert.False(analysis.IsOverride);
        }

        [Fact]
        public void WhenRecordFound_ReturnsRecordInfo()
        {
            groups.AddRecords<Container>(
                "plugin.esp",
                x => x.EditorID = "container1",
                x => x.EditorID = "container2");
            var analysis = analyzer.Analyze("plugin.esp", new RecordKey("plugin.esp", "000002"));

            Assert.Equal("plugin.esp", analysis.BasePluginName);
            Assert.Equal("000002", analysis.LocalFormIdHex);
            Assert.Equal(recordType, analysis.Type);
            Assert.True(analysis.Exists);
            Assert.Equal("container2", analysis.EditorId);
            Assert.False(analysis.IsInjectedOrInvalid);
            Assert.False(analysis.IsOverride);
        }

        [Fact]
        public void WhenRecordKeyIsOtherPlugin_IsOverride_IsTrue()
        {
            var analysis = analyzer.Analyze("plugin.esp", new RecordKey("master.esp", "000001"));

            Assert.True(analysis.IsOverride);
        }

        [Fact]
        public void WhenMasterHasRecord_IsInjectedOrInvalid_IsFalse()
        {
            groups.AddRecords<Container>("master.esp", x => { });
            var analysis = analyzer.Analyze("plugin.esp", new RecordKey("master.esp", "000001"));

            Assert.True(analysis.IsOverride);
        }

        [Fact]
        public void WhenMasterMissingRecord_IsInjectedOrInvalid_IsTrue()
        {
            var analysis = analyzer.Analyze("plugin.esp", new RecordKey("master.esp", "000001"));

            Assert.True(analysis.IsInjectedOrInvalid);
        }
    }
}
