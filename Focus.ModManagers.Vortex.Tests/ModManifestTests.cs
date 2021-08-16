using System.Linq;
using System.IO.Abstractions.TestingHelpers;
using Xunit;

namespace Focus.ModManagers.Vortex.Tests
{
    public class ModManifestTests
    {
        private readonly MockFileSystem fs;

        public ModManifestTests()
        {
            fs = new MockFileSystem();
        }

        [Fact]
        public void ParsesValidManifest()
        {
            const string json = @"
            {
                ""files"": {
                    ""first folder"": {
                        ""modId"": 123
                    },
                    ""second folder"": {
                        ""modId"": 456
                    },
                    ""third folder"": {
                        ""modId"": ""123""
                    }
                },
                ""mods"": {
                    ""123"": {
                        ""name"": ""Mod 1""
                    },
                    ""456"": {
                        ""name"": ""Mod 2""
                    }
                },
                ""reportPath"": ""C:\\temp\\vortexreport.json"",
                ""stagingDir"": ""C:\\vortex\\staging""
            }";
            fs.AddFile(@"C:\temp\vortex-bootstrap.json", new MockFileData(json));
            var manifest = ModManifest.LoadFromFile(fs, @"C:\temp\vortex-bootstrap.json");

            Assert.Equal(@"C:\vortex\staging", manifest.StagingDir);
            Assert.Equal(@"C:\vortex\staging", manifest.ModsDirectory);
            Assert.Collection(
                manifest.Files,
                x =>
                {
                    Assert.Equal("first folder", x.Key);
                    Assert.Equal("123", x.Value.ModId);
                },
                x =>
                {
                    Assert.Equal("second folder", x.Key);
                    Assert.Equal("456", x.Value.ModId);
                },
                x =>
                {
                    Assert.Equal("third folder", x.Key);
                    Assert.Equal("123", x.Value.ModId);
                });
            Assert.Collection(
                manifest.Mods,
                x =>
                {
                    Assert.Equal("123", x.Key);
                    Assert.Equal("Mod 1", x.Value.Name);
                },
                x =>
                {
                    Assert.Equal("456", x.Key);
                    Assert.Equal("Mod 2", x.Value.Name);
                });
        }
    }
}
