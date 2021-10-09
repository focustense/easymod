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
                        ""isEnabled"": true,
                        ""modId"": 123,
                    },
                    ""second folder"": {
                        ""id"": 777,
                        ""modId"": 456
                    },
                    ""third folder"": {
                        ""id"": 888,
                        ""modId"": ""123"",
                        ""isEnabled"": false,
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
                ""gameDataPath"": ""C:\\games\\steam\\steamapps\\common\\Skyrim Special Edition\\data"",
                ""reportPath"": ""C:\\temp\\vortexreport.json"",
                ""stagingDir"": ""C:\\vortex\\staging""
            }";
            fs.AddFile(@"C:\temp\vortex-bootstrap.json", new MockFileData(json));
            var manifest = ModManifest.LoadFromFile(fs, @"C:\temp\vortex-bootstrap.json");

            Assert.Equal(@"C:\games\steam\steamapps\common\Skyrim Special Edition\data", manifest.GameDataPath);
            Assert.Equal(@"C:\vortex\staging", manifest.StagingDir);
            Assert.Equal(@"C:\vortex\staging", manifest.ModsDirectory);
            Assert.Collection(
                manifest.Files,
                x =>
                {
                    Assert.Equal("first folder", x.Key);
                    Assert.Null(x.Value.Id);
                    Assert.Equal("123", x.Value.ModId);
                    Assert.True(x.Value.IsEnabled);
                },
                x =>
                {
                    Assert.Equal("second folder", x.Key);
                    Assert.Equal("777", x.Value.Id);
                    Assert.Equal("456", x.Value.ModId);
                    Assert.Null(x.Value.IsEnabled);
                },
                x =>
                {
                    Assert.Equal("third folder", x.Key);
                    Assert.Equal("888", x.Value.Id);
                    Assert.Equal("123", x.Value.ModId);
                    Assert.False(x.Value.IsEnabled);
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
