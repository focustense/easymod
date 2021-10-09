using Focus.Analysis.Plugins;
using Focus.Analysis.Records;
using Focus.Apps.EasyNpc.Build;
using Focus.Apps.EasyNpc.Build.Checks;
using Focus.Apps.EasyNpc.Profiles;
using Focus.Testing.Files;
using Moq;
using System.Linq;
using Xunit;

namespace Focus.Apps.EasyNpc.Tests.Build.Checks
{
    public class BadArchivesTest
    {
        private const string DataPath = @"C:\game\data\";

        private readonly FakeArchiveProvider archiveProvider;
        private readonly BadArchives check;

        private int nextFormId = 123456;

        public BadArchivesTest()
        {
            archiveProvider = new FakeArchiveProvider();
            check = new(archiveProvider);
        }

        [Fact]
        public void WhenBadArchivesReferenced_YieldsWarnings()
        {
            archiveProvider.BadArchivePaths = new[]
            {
                $"{DataPath}plugin1.bsa",
                $"{DataPath}plugin2 - Textures.bsa",
                $"{DataPath}plugin3.bsa",
                $"{DataPath}plugin8.bsa",
                $"{DataPath}plugin9.bsa",
            };
            var npcs = new[]
            {
                CreateMockNpc("plugin1.esp"),
                CreateMockNpc("plugin2.esp"),
                CreateMockNpc("plugin3.esp"),
                CreateMockNpc("plugin4.esp")
            };
            var profile = new Profile(npcs);
            var settings = new BuildSettings(profile, "", "");

            var warnings = check.Run(profile, settings).ToList();

            Assert.Collection(
                warnings,
                x =>
                {
                    Assert.Equal(BuildWarningId.BadArchive, x.Id);
                    Assert.Equal(WarningMessages.BadArchive("plugin1.bsa"), x.Message);
                },
                x =>
                {
                    Assert.Equal(BuildWarningId.BadArchive, x.Id);
                    Assert.Equal(WarningMessages.BadArchive("plugin2 - Textures.bsa"), x.Message);
                },
                x =>
                {
                    Assert.Equal(BuildWarningId.BadArchive, x.Id);
                    Assert.Equal(WarningMessages.BadArchive("plugin3.bsa"), x.Message);
                });
        }

        [Fact]
        public void WhenBadArchivesNotReferenced_Ignores()
        {
            archiveProvider.BadArchivePaths = new[] { $"{DataPath}plugin1.bsa", $"{DataPath}plugin2.bsa" };
            var npcs = new[] { CreateMockNpc("plugin3.esp"), CreateMockNpc("plugin4.esp") };
            var profile = new Profile(npcs);
            var settings = new BuildSettings(profile, "", "");

            var warnings = check.Run(profile, settings).ToList();

            Assert.Empty(warnings);
        }

        private INpc CreateMockNpc(string facePluginName)
        {
            var npcMock = new Mock<INpc>();
            npcMock.SetupGet(x => x.BasePluginName).Returns("base.esp");
            npcMock.SetupGet(x => x.LocalFormIdHex).Returns(nextFormId++.ToString());
            npcMock.SetupGet(x => x.HasAvailableFaceCustomizations).Returns(true);
            var option = new NpcOption(new Sourced<NpcAnalysis>(facePluginName, new()), false);
            npcMock.SetupGet(x => x.FaceOption).Returns(option);
            return npcMock.Object;
        }
    }
}
