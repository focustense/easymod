﻿using IniParser.Model;
using Moq;
using System;
using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Focus.ModManagers.ModOrganizer.Tests
{
    public class ModOrganizerComponentResolverTests
    {
        private const string RootPath = @"C:\Test\Mods";

        private readonly ModOrganizerComponentResolver componentResolver;
        private readonly Mock<IModOrganizerConfiguration> configurationMock;
        private readonly MockFileSystem fs;

        public ModOrganizerComponentResolverTests()
        {
            configurationMock = new Mock<IModOrganizerConfiguration>();
            fs = new MockFileSystem();
            componentResolver = new ModOrganizerComponentResolver(fs, configurationMock.Object, RootPath);
        }

        [Fact]
        public void WhenMetaIniNotFound_ResolvesDefaultComponent()
        {
            var component = componentResolver.ResolveComponentInfo("foo");

            Assert.Equal("foo", component.Id);
            Assert.Equal("foo", component.Name);
            Assert.Equal(new ModLocatorKey("", "foo"), component.ModKey);
            Assert.Equal(@$"{RootPath}\foo", component.Path);
            Assert.True(component.IsEnabled);
        }

        [Fact]
        public void WhenMetaIniIsEmpty_ResolvesDefaultComponent()
        {
            fs.AddFile(@$"{RootPath}\foo\meta.ini", new MockFileData(""));
            var component = componentResolver.ResolveComponentInfo("foo");

            Assert.Equal("foo", component.Id);
            Assert.Equal("foo", component.Name);
            Assert.Equal(new ModLocatorKey("", "foo"), component.ModKey);
            Assert.Equal(@$"{RootPath}\foo", component.Path);
            Assert.True(component.IsEnabled);
        }

        [Fact]
        public void WhenMetaProvidesModInfo_AndNoDownloadAvailable_ReturnsDefaultWithId()
        {
            var ini = new IniData();
            ini.AddSection("General", new()
            {
                { "modid", "318" },
                { "installationFile", "foo_file.7z" },
            });
            ini.AddSection("installedFiles", new()
            {
                { @"1\fileid", "12345" },
            });
            fs.AddFile(@$"{RootPath}\foo\meta.ini", new MockFileData(ini.ToString()));
            var component = componentResolver.ResolveComponentInfo("foo");

            Assert.Equal("12345", component.Id);
            Assert.Equal("foo", component.Name);
            Assert.Equal(new ModLocatorKey("318", ""), component.ModKey);
            Assert.Equal(@$"{RootPath}\foo", component.Path);
            Assert.True(component.IsEnabled);
        }

        [Fact]
        public void WhenMetaProvidesModInfo_AndLinksToValidDownload_ReturnsFullModInfo()
        {
            const string downloadDirectory = @"D:\Mod Organizer Downloads";
            configurationMock.SetupGet(x => x.DownloadDirectory).Returns(downloadDirectory);
            var metaIni = new IniData();
            metaIni.AddSection("General", new()
            {
                { "modid", "318" },
                { "installationFile", "foo_file.7z" },
            });
            fs.AddFile(@$"{RootPath}\foo\meta.ini", new MockFileData(metaIni.ToString()));
            var downloadIni = new IniData();
            downloadIni.AddSection("General", new()
            {
                { "fileID", "8372" },
                { "modName", "My Awesome Mod" },
            });
            fs.AddFile($@"{downloadDirectory}\foo_file.7z.meta", downloadIni.ToString());
            var component = componentResolver.ResolveComponentInfo("foo");

            Assert.Equal("8372", component.Id);
            Assert.Equal("foo", component.Name);
            Assert.Equal(new ModLocatorKey("318", "My Awesome Mod"), component.ModKey);
            Assert.Equal(@$"{RootPath}\foo", component.Path);
            Assert.True(component.IsEnabled);
        }

        [Fact]
        public void WhenComponentNameIsBackup_AndMetaIniNotFound_ReturnsDisabledComponent()
        {
            var component = componentResolver.ResolveComponentInfo("foo_backup4");

            Assert.False(component.IsEnabled);
        }

        [Fact]
        public void WhenComponentNameIsBackup_AndMetaProvidesModInfo_ReturnsDisabledComponent()
        {
            var ini = new IniData();
            ini.AddSection("General", new() { { "modid", "17436" } });
            fs.AddFile(@$"{RootPath}\foo\meta.ini", new MockFileData(ini.ToString()));
            var component = componentResolver.ResolveComponentInfo("foo_backup26");

            Assert.False(component.IsEnabled);
        }
    }
}
