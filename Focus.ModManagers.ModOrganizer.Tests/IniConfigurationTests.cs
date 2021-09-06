using IniParser.Model;
using Xunit;

namespace Focus.ModManagers.ModOrganizer.Tests
{
    public class IniConfigurationTests
    {
        private const string DefaultBaseDirectory = @"C:\Games\Mod Organizer";

        private readonly IniData iniData;

        public IniConfigurationTests()
        {
            iniData = new IniData();            
        }

        [Fact]
        public void WhenNoSettingsSpecified_UsesDefaultDirectories()
        {
            var config = new IniConfiguration(iniData, DefaultBaseDirectory);

            Assert.Equal(DefaultBaseDirectory, config.BaseDirectory);
            Assert.Equal($@"{DefaultBaseDirectory}\downloads", config.DownloadDirectory);
            Assert.Equal($@"{DefaultBaseDirectory}\mods", config.ModsDirectory);
            Assert.Equal($@"{DefaultBaseDirectory}\overwrite", config.OverwriteDirectory);
            Assert.Equal($@"{DefaultBaseDirectory}\profiles", config.ProfilesDirectory);
        }

        [Fact]
        public void WhenBaseDirectorySpecified_UsesRelativeDirectories()
        {
            iniData.AddSection("Settings", new() { { "base_directory", @"D:\SSE\ModOrganizer" } });
            var config = new IniConfiguration(iniData, DefaultBaseDirectory);

            Assert.Equal(@"D:\SSE\ModOrganizer", config.BaseDirectory);
            Assert.Equal(@"D:\SSE\ModOrganizer\downloads", config.DownloadDirectory);
            Assert.Equal(@"D:\SSE\ModOrganizer\mods", config.ModsDirectory);
            Assert.Equal(@"D:\SSE\ModOrganizer\overwrite", config.OverwriteDirectory);
            Assert.Equal(@"D:\SSE\ModOrganizer\profiles", config.ProfilesDirectory);
        }

        [Fact]
        public void WhenIndividualDirectoriesSpecified_UsesConfiguredDirectories()
        {
            iniData.AddSection("Settings", new() {
                { "base_directory", @"D:\SSE\ModOrganizer" },
                { "download_directory", @"E:\Downloads\Mod Organizer" },
                { "mod_directory", @"%BASE_DIR%\altmods" },
                { "profiles_directory", @"%BASE_DIR%\data\profiles" },
            });
            var config = new IniConfiguration(iniData, DefaultBaseDirectory);

            Assert.Equal(@"D:\SSE\ModOrganizer", config.BaseDirectory);
            Assert.Equal(@"E:\Downloads\Mod Organizer", config.DownloadDirectory);
            Assert.Equal(@"D:\SSE\ModOrganizer\altmods", config.ModsDirectory);
            Assert.Equal(@"D:\SSE\ModOrganizer\overwrite", config.OverwriteDirectory);
            Assert.Equal(@"D:\SSE\ModOrganizer\data\profiles", config.ProfilesDirectory);
        }

        [Fact]
        public void WhenSelectedProfileIsByteArrayWrapped_ReadsSelectedProfile()
        {
            iniData.AddSection("General", new() { { "selected_profile", "@ByteArray(My Profile)" } });
            var config = new IniConfiguration(iniData, DefaultBaseDirectory);

            Assert.Equal("My Profile", config.SelectedProfileName);
        }

        [Fact]
        public void WhenSelectedProfileIsPlainText_ReadsSelectedProfile()
        {
            iniData.AddSection("General", new() { { "selected_profile", "My Profile" } });
            var config = new IniConfiguration(iniData, DefaultBaseDirectory);

            Assert.Equal("My Profile", config.SelectedProfileName);
        }

        [Fact]
        public void WhenGamePathIsByteArrayWrapped_ReadsGamePath()
        {
            iniData.AddSection("General", new() { { "gamePath", @"@ByteArray(C:\\Games\\Wabbajack\\Serenity 2\\Stock Game)" } });
            var config = new IniConfiguration(iniData, DefaultBaseDirectory);

            Assert.Equal(@"C:\Games\Wabbajack\Serenity 2\Stock Game\data", config.GameDataPath);
        }

        [Fact]
        public void WhenGamePathIsPlainText_ReadsGamePath()
        {
            iniData.AddSection("General", new() { { "gamePath", @"C:\Games\Steam\steamapps\common\Skyrim Special Edition" } });
            var config = new IniConfiguration(iniData, DefaultBaseDirectory);

            Assert.Equal(@"C:\Games\Steam\steamapps\common\Skyrim Special Edition\data", config.GameDataPath);
        }
    }
}
