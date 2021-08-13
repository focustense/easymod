using Moq;
using Mutagen.Bethesda;
using Noggog;
using Xunit;

namespace Focus.Providers.Mutagen.Tests
{
    public class GameInstanceTests
    {
        delegate void TryGetDataFolderCallback(GameRelease gameRelease, out DirectoryPath path);

        private readonly IGameLocations gameLocations;
        private readonly Mock<IGameLocations> gameLocationsMock;

        public GameInstanceTests()
        {
            gameLocationsMock = new Mock<IGameLocations>();
            gameLocations = gameLocationsMock.Object;
        }

        [Fact]
        public void WhenInvalidGameId_ThrowsUnsupportedGame()
        {
            Assert.Throws<UnsupportedGameException>(() => GameInstance.FromGameId(gameLocations, "invalid"));
        }

        [Fact]
        public void WhenValidGameId_AndNoInstallDetected_AndDirectoryNotSpecified_ThrowsMissingGameData()
        {
            Assert.Throws<MissingGameDataException>(() => GameInstance.FromGameId(gameLocations, "SkyrimSE"));
        }

        [Fact]
        public void WhenValidGameId_AndNoInstallDetected_AndDirectorySpecified_UsesCustomDirectory()
        {
            var game = GameInstance.FromGameId(gameLocations, "SkyrimSE", @"C:\custom\path");

            Assert.Equal(GameRelease.SkyrimSE, game.GameRelease);
            Assert.Equal(@"C:\custom\path", game.DataDirectory);
        }

        [Fact]
        public void WhenValidGameId_AndInstallDetected_AndDirectoryNotSpecified_UsesDetectedDirectory()
        {
            gameLocationsMock.Setup(x => x.TryGetDataFolder(GameRelease.SkyrimSE, out It.Ref<DirectoryPath>.IsAny))
                .Callback(new TryGetDataFolderCallback((GameRelease gameRelease, out DirectoryPath path) =>
                {
                    path = @"C:\path\to\game";
                }))
                .Returns(true);
            var game = GameInstance.FromGameId(gameLocations, "SkyrimSE");

            Assert.Equal(GameRelease.SkyrimSE, game.GameRelease);
            Assert.Equal(@"C:\path\to\game", game.DataDirectory);
        }

        [Fact]
        public void WhenValidGameId_AndInstallDetected_AndDirectorySpecified_UsesCustomDirectory()
        {
            gameLocationsMock.Setup(x => x.TryGetDataFolder(GameRelease.SkyrimSE, out It.Ref<DirectoryPath>.IsAny))
                .Callback(new TryGetDataFolderCallback((GameRelease gameRelease, out DirectoryPath path) =>
                {
                    path = @"C:\path\to\game";
                }))
                .Returns(true);
            var game = GameInstance.FromGameId(gameLocations, "SkyrimSE", @"C:\custom\path");

            Assert.Equal(GameRelease.SkyrimSE, game.GameRelease);
            Assert.Equal(@"C:\custom\path", game.DataDirectory);
        }
    }
}
