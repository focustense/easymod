using Focus.Analysis.Plugins;
using Focus.Analysis.Records;
using Xunit;

namespace Focus.Analysis.Tests
{
    public class RecordAnalysisChainTests
    {
        private const RecordType recordType = RecordType.Climate;

        private readonly RecordAnalysisChain<FakeRecordAnalysis> chain;

        public RecordAnalysisChainTests()
        {
            chain = new RecordAnalysisChain<FakeRecordAnalysis>(new[]
            {
                new Sourced<FakeRecordAnalysis>(
                    "plugin2",
                    new(recordType) { BasePluginName = "plugin1", LocalFormIdHex = "123456", EditorId = "plugin2_id" }),
                new Sourced<FakeRecordAnalysis>(
                    "plugin1",
                    new(recordType) { BasePluginName = "plugin1", LocalFormIdHex = "123456", EditorId = "plugin1_id" }),
                new Sourced<FakeRecordAnalysis>(
                    "plugin3",
                    new(recordType) { BasePluginName = "plugin1", LocalFormIdHex = "123456", EditorId = "plugin3_id" }),
                new Sourced<FakeRecordAnalysis>(
                    "plugin4",
                    new(recordType) { BasePluginName = "plugin1", LocalFormIdHex = "123456", EditorId = "plugin4_id" }),
            });
        }

        [Fact]
        public void Count_IsNumberOfPluginsUsed()
        {
            Assert.Equal(4, chain.Count);
        }

        [Fact]
        public void IndexerByNumber_ReturnsItemAtIndex()
        {
            Assert.Equal("plugin3", chain[2].PluginName);
        }

        [Fact]
        public void IndexerByString_ReturnsRecordForPlugin()
        {
            Assert.Equal("plugin2_id", chain["plugin2"].EditorId);
        }

        [Fact]
        public void Key_IsAnyRecordKey()
        {
            Assert.Equal(new RecordKey("plugin1", "123456"), chain.Key);
        }

        [Fact]
        public void Master_IsRecordForKeyBasePlugin()
        {
            Assert.Equal("plugin1_id", chain.Master.EditorId);
        }

        [Fact]
        public void Winner_IsLastRecord()
        {
            Assert.Equal("plugin4_id", chain.Winner.EditorId);
        }

        [Fact]
        public void CanEnumerateAllItems()
        {
            Assert.Collection(
                chain,
                x => Assert.Equal("plugin2_id", x.Analysis.EditorId),
                x => Assert.Equal("plugin1_id", x.Analysis.EditorId),
                x => Assert.Equal("plugin3_id", x.Analysis.EditorId),
                x => Assert.Equal("plugin4_id", x.Analysis.EditorId));
        }
    }
}
