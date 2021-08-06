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
            Assert.Equal("plugin2_id", chain["pluGiN2"].Analysis.EditorId);
        }

        [Fact]
        public void IndexOfItem_ReturnsPositionInChain()
        {
            Assert.Equal(-1, chain.IndexOf(new Sourced<FakeRecordAnalysis>("unknown", new(RecordType.Action))));
            Assert.Equal(0, chain.IndexOf(chain[0]));
            Assert.Equal(1, chain.IndexOf(chain[1]));
            Assert.Equal(2, chain.IndexOf(chain[2]));
            Assert.Equal(3, chain.IndexOf(chain[3]));
        }

        [Fact]
        public void IndexOfString_ReturnsPositionWithPluginName()
        {
            Assert.Equal(-1, chain.IndexOf("unknown"));
            Assert.Equal(0, chain.IndexOf(chain[0].PluginName));
            Assert.Equal(1, chain.IndexOf(chain[1].PluginName));
            Assert.Equal(2, chain.IndexOf(chain[2].PluginName.ToUpper()));
            Assert.Equal(3, chain.IndexOf(chain[3].PluginName));
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

        [Fact]
        public void WhenIncludesPlugin_Contains_ReturnsTrue()
        {
            Assert.True(chain.Contains("plugin1"));
            Assert.True(chain.Contains("PlUgIn3"));
        }

        [Fact]
        public void WhenMissingPlugin_Contains_ReturnsFalse()
        {
            Assert.False(chain.Contains("nope"));
        }
    }
}
