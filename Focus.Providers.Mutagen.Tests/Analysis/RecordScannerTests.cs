using Focus.Providers.Mutagen.Analysis;
using Moq;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Noggog;
using System;
using System.Linq;
using Xunit;
using RecordType = Focus.Analysis.Records.RecordType;

namespace Focus.Providers.Mutagen.Tests.Analysis
{
    public class RecordScannerTests
    {
        private readonly Mock<IGroupCache> groupCacheMock;
        private readonly RecordScanner scanner;

        public RecordScannerTests()
        {
            groupCacheMock = new Mock<IGroupCache>();
            scanner = new RecordScanner(groupCacheMock.Object);
        }

        [Fact]
        public void WhenGroupNotFound_GetKeys_ReturnsEmpty()
        {
            Assert.Equal(Enumerable.Empty<IRecordKey>(), scanner.GetKeys("plugin.esp", RecordType.HeadPart));
        }

        [Fact]
        public void WhenGroupFound_GetKeys_ReturnsKeysInGroup()
        {
            var headPartCacheMock = new Mock<IReadOnlyCache<IHeadPartGetter, FormKey>>();
            groupCacheMock.Setup(x => x.Get("plugin.esp", RecordType.HeadPart)).Returns(headPartCacheMock.Object);
            headPartCacheMock.SetupGet(x => x.Keys).Returns(new[]
            {
                FormKey.Factory("000001:plugin.esp"),
                FormKey.Factory("000002:plugin.esp"),
                FormKey.Factory("000003:plugin.esp"),
            });

            Assert.Equal(
                new[]
                {
                    new RecordKey("plugin.esp", "000001"),
                    new RecordKey("plugin.esp", "000002"),
                    new RecordKey("plugin.esp", "000003"),
                },
                scanner.GetKeys("plugin.esp", RecordType.HeadPart));
        }
    }
}
