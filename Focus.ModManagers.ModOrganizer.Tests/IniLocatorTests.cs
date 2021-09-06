using Focus.Abstractions.Windows;
using Microsoft.Win32;
using Moq;
using System.IO.Abstractions.TestingHelpers;
using Xunit;
using static System.Environment;

namespace Focus.ModManagers.ModOrganizer.Tests
{
    public class IniLocatorTests
    {
        private const string LocalAppDataPath = @"C:\Test\LocalAppData";
        private const string PortablePath = @"C:\Games\Mod Organizer Portable";

        private static readonly string PortableExePath = $@"{PortablePath}\ModOrganizer.exe";
        private static readonly string PortableIniPath = $@"{PortablePath}\ModOrganizer.ini";

        private readonly Mock<IEnvironmentStatics> environmentMock;
        private readonly MockFileSystem fs;
        private readonly Mock<IRegistryKey> hkcuMock;
        private readonly IniLocator locator;
        private readonly Mock<IRegistryKeyStatics> registryMock;

        public IniLocatorTests()
        {
            environmentMock = new Mock<IEnvironmentStatics>();
            environmentMock.Setup(x => x.GetFolderPath(SpecialFolder.LocalApplicationData)).Returns(LocalAppDataPath);
            registryMock = new Mock<IRegistryKeyStatics>();
            hkcuMock = new Mock<IRegistryKey>();
            registryMock.Setup(x => x.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default))
                .Returns(hkcuMock.Object);
            fs = new MockFileSystem();
            locator = new IniLocator(environmentMock.Object, registryMock.Object, fs);
        }

        [Fact]
        public void WhenDefaultKeyExists_UsesNamedInstance()
        {
            // Include legacy subkey here only to prove that it is ignored.
            SetupSubkey(@"SOFTWARE\Tannin\Mod Organizer", "CurrentInstance", "Old Instance");
            SetupSubkey(@"SOFTWARE\Mod Organizer Team\Mod Organizer", "CurrentInstance", "My Instance");

            var iniPath = locator.DetectIniPath(PortableExePath);
            Assert.Equal(@$"{LocalAppDataPath}\ModOrganizer\My Instance\ModOrganizer.ini", iniPath);
        }

        [Fact]
        public void WhenDefaultKeyEmpty_UsesPortableInstance()
        {
            // Include legacy subkey here only to prove that it is ignored.
            SetupSubkey(@"SOFTWARE\Tannin\Mod Organizer", "CurrentInstance", "Old Instance");
            SetupSubkey(@"SOFTWARE\Mod Organizer Team\Mod Organizer", "CurrentInstance", "");

            var iniPath = locator.DetectIniPath(PortableExePath);
            Assert.Equal(PortableIniPath, iniPath);
        }

        [Fact]
        public void WhenOnlyLegacyKeyExists_UsesNamedInstance()
        {
            SetupSubkey(@"SOFTWARE\Tannin\Mod Organizer", "CurrentInstance", "Old Instance");

            var iniPath = locator.DetectIniPath(PortableExePath);
            Assert.Equal(@$"{LocalAppDataPath}\ModOrganizer\Old Instance\ModOrganizer.ini", iniPath);
        }

        [Fact]
        public void WhenLegacyKeyEmpty_UsesPortableInstance()
        {
            SetupSubkey(@"SOFTWARE\Tannin\Mod Organizer", "CurrentInstance", "");

            var iniPath = locator.DetectIniPath(PortableExePath);
            Assert.Equal(PortableIniPath, iniPath);
        }

        [Fact]
        public void WhenNoKeysExist_UsesPortableInstance()
        {
            var iniPath = locator.DetectIniPath(PortableExePath);
            Assert.Equal(PortableIniPath, iniPath);
        }

        [Fact]
        public void WhenPortableOverrideFileExists_AlwaysUsesPortableInstance()
        {
            SetupSubkey(@"SOFTWARE\Tannin\Mod Organizer", "CurrentInstance", "Old Instance");
            SetupSubkey(@"SOFTWARE\Mod Organizer Team\Mod Organizer", "CurrentInstance", "My Instance");
            fs.AddFile(@$"{PortablePath}\Portable.txt", new MockFileData(""));

            var iniPath = locator.DetectIniPath(PortableExePath);
            Assert.Equal(PortableIniPath, iniPath);
        }

        private void SetupSubkey(string key, string name, string value)
        {
            var subKeyMock = new Mock<IRegistryKey>();
            subKeyMock.Setup(x => x.GetValue(name, It.IsAny<object>())).Returns(value);
            hkcuMock.Setup(x => x.OpenSubKey(key)).Returns(subKeyMock.Object);
        }
    }
}
