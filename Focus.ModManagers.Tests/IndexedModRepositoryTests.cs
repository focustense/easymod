using Focus.Testing.Files;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace Focus.ModManagers.Tests
{
    public class IndexedModRepositoryTests
    {
        private const string RootPath = @"C:\root";

        private readonly FakeArchiveProvider archiveProvider;
        private readonly Mock<IComponentResolver> componentResolverMock;
        private readonly FakeBucketedFileIndex modIndex;
        private readonly Dictionary<string, string> modNames;
        private readonly IndexedModRepository modRepository;

        public IndexedModRepositoryTests()
        {
            modIndex = new FakeBucketedFileIndex();
            modIndex.AddFiles("01_foo", "common/c1", "foo1", "foo2");
            modIndex.AddFiles("01_bar", "bar1", "bar2", "bar.bsa");
            modIndex.AddFiles("02_baz", "common/c1", "baz1", "baz2");
            modIndex.AddFiles("02_quux_disabled", "common/c1", "quux1", "quux2");
            modIndex.AddFiles("standalone", "foo2", "bar2");
            archiveProvider = new FakeArchiveProvider();
            archiveProvider.AddFiles(@$"{RootPath}\01_bar\bar.bsa", "bar3", "common/c1");
            componentResolverMock = new Mock<IComponentResolver>();
            componentResolverMock.Setup(x => x.ResolveComponentInfo(It.IsAny<string>()))
                .Returns((string componentName) => new ModComponentInfo(
                    componentName.IndexOf('_') == 2 ?
                        new ModLocatorKey(componentName[0..2], modNames[componentName[0..2]]) :
                        ModLocatorKey.Empty,
                    componentName.IndexOf('_') == 2 ? $"{componentName[3..]}_id" : $"{componentName}_id",
                    componentName.IndexOf('_') == 2 ? componentName[3..] : componentName,
                    Path.Combine(RootPath, componentName),
                    !componentName.EndsWith("_disabled")));
            modNames = new()
            {
                { "01", "modname1" },
                { "02", "modname2" },
            };
            modRepository = new IndexedModRepository(
                modIndex, archiveProvider, componentResolverMock.Object, RootPath);
        }

        [Theory]
        [InlineData("foo", "modname1")]
        [InlineData("bar", "modname1")]
        [InlineData("baz", "modname2")]
        [InlineData("quux_disabled", "modname2")]
        [InlineData("standalone", "standalone")]
        [InlineData("unknown", null)]
        public void CanFindModByComponentName(string componentName, string expectedModName)
        {
            var mod = modRepository.FindByComponentName(componentName);
            Assert.Equal(expectedModName, mod?.Name);
        }

        [Theory]
        [InlineData("01_foo", "modname1")]
        [InlineData("01_bar", "modname1")]
        [InlineData("02_baz", "modname2")]
        [InlineData("02_quux_disabled", "modname2")]
        [InlineData("standalone", "standalone")]
        [InlineData("unknown", null)]
        public void CanFindModByComponentPath(string pathInRoot, string expectedModName)
        {
            var mod = modRepository.FindByComponentPath($@"{RootPath}\{pathInRoot}");
            Assert.Equal(expectedModName, mod?.Name);
        }

        [Fact]
        public void Enumerator_YieldsConsolidatedMods()
        {
            Assert.Collection(
                modRepository,
                mod =>
                {
                    Assert.Equal("01", mod.Id);
                    Assert.Equal("modname1", mod.Name);
                    Assert.Collection(
                        mod.Components,
                        x =>
                        {
                            Assert.Equal("foo_id", x.Id);
                            Assert.Equal("foo", x.Name);
                            Assert.Equal($@"{RootPath}\01_foo", x.Path);
                            Assert.Equal(new ModLocatorKey("01", "modname1"), x.ModKey);
                            Assert.True(x.IsEnabled);
                        },
                        x =>
                        {
                            Assert.Equal("bar_id", x.Id);
                            Assert.Equal("bar", x.Name);
                            Assert.Equal($@"{RootPath}\01_bar", x.Path);
                            Assert.Equal(new ModLocatorKey("01", "modname1"), x.ModKey);
                            Assert.True(x.IsEnabled);
                        });
                },
                mod =>
                {
                    Assert.Equal("02", mod.Id);
                    Assert.Equal("modname2", mod.Name);
                    Assert.Collection(
                        mod.Components,
                        x =>
                        {
                            Assert.Equal("baz_id", x.Id);
                            Assert.Equal("baz", x.Name);
                            Assert.Equal($@"{RootPath}\02_baz", x.Path);
                            Assert.Equal(new ModLocatorKey("02", "modname2"), x.ModKey);
                            Assert.True(x.IsEnabled);
                        },
                        x =>
                        {
                            Assert.Equal("quux_disabled_id", x.Id);
                            Assert.Equal("quux_disabled", x.Name);
                            Assert.Equal($@"{RootPath}\02_quux_disabled", x.Path);
                            Assert.Equal(new ModLocatorKey("02", "modname2"), x.ModKey);
                            Assert.False(x.IsEnabled);
                        });
                },
                mod =>
                {
                    Assert.Equal(string.Empty, mod.Id);
                    Assert.Equal("standalone", mod.Name);
                    Assert.Collection(
                        mod.Components,
                        x =>
                        {
                            Assert.Equal("standalone_id", x.Id);
                            Assert.Equal("standalone", x.Name);
                            Assert.Equal($@"{RootPath}\standalone", x.Path);
                            Assert.Equal(ModLocatorKey.Empty, x.ModKey);
                            Assert.True(x.IsEnabled);
                        });
                });
        }

        [Theory]
        [InlineData("02", "", "02", "modname2")]
        [InlineData("02", "invalid", "02", "modname2")]
        [InlineData("02", "modname1", "02", "modname2")]
        [InlineData("", "modname1", "01", "modname1")]
        [InlineData("99", "modname1", "01", "modname1")]
        [InlineData("", "bar", "01", "modname1")]
        [InlineData("99", "baz", "02", "modname2")]
        [InlineData("99", "standalone", "", "standalone")]
        public void FindByKey_PrioritizesModId_ThenModName_Then_ComponentName(
            string keyId, string keyName, string expectedModId, string expectedModName)
        {
            var mod = modRepository.FindByKey(new ModLocatorKey(keyId, keyName));
            Assert.Equal(expectedModId, mod?.Id);
            Assert.Equal(expectedModName, mod?.Name);
        }

        [Theory]
        [InlineData("01", "modname1")]
        [InlineData("02", "modname2")]
        [InlineData("", null)]
        [InlineData("99", null)]
        public void GetById_FindsModWithId(string modId, string expectedModName)
        {
            var mod = modRepository.GetById(modId);
            Assert.Equal(expectedModName, mod?.Name);
        }

        [Theory]
        [InlineData("modname1", "01")]
        [InlineData("foo", "01")]
        [InlineData("modname2", "02")]
        [InlineData("quux_disabled", "02")]
        [InlineData("standalone", "")]
        [InlineData("", null)]
        [InlineData("invalid", null)]
        public void GetByName_FindsModOrComponentWithName(string modName, string expectedModId)
        {
            var mod = modRepository.GetByName(modName);
            Assert.Equal(expectedModId, mod?.Id);
        }

        [Fact]
        public void SearchForFiles_Default_YieldsLooseMatchesInEnabledComponents()
        {
            Assert.Collection(
                modRepository.SearchForFiles("common/c1", false, false),
                x => AssertSearchResult(x, "modname1", "foo"),
                x => AssertSearchResult(x, "modname2", "baz"));
            Assert.Collection(
                modRepository.SearchForFiles("foo2", false, false),
                x => AssertSearchResult(x, "modname1", "foo"),
                x => AssertSearchResult(x, "", "standalone"));
            Assert.Collection(
                modRepository.SearchForFiles("bar.bsa", false, false),
                x => AssertSearchResult(x, "modname1", "bar"));

            Assert.Empty(modRepository.SearchForFiles("bar3", false, false));
            Assert.Empty(modRepository.SearchForFiles("quux1", false, false));
        }

        [Fact]
        public void SearchForFiles_WhenArchivesRequested_YieldsMatchesInArchives()
        {
            Assert.Collection(
                modRepository.SearchForFiles("common/c1", true, false),
                x => AssertSearchResult(x, "modname1", "foo"),
                x => AssertSearchResult(x, "modname2", "baz"),
                x => AssertSearchResult(x, "modname1", "bar", "bar.bsa"));
            Assert.Collection(
                modRepository.SearchForFiles("bar3", true, false),
                x => AssertSearchResult(x, "modname1", "bar", "bar.bsa"));
        }

        [Fact]
        public void SearchForFiles_WhenDisabledRequested_YieldsMatchesInDisabledComponents()
        {
            Assert.Collection(
                modRepository.SearchForFiles("common/c1", false, true),
                x => AssertSearchResult(x, "modname1", "foo"),
                x => AssertSearchResult(x, "modname2", "baz"),
                x => AssertSearchResult(x, "modname2", "quux_disabled"));
            Assert.Collection(
                modRepository.SearchForFiles("quux1", false, true),
                x => AssertSearchResult(x, "modname2", "quux_disabled"));
        }

        [Fact]
        public void WhenArchiveAdded_IndexesNewArchive()
        {
            archiveProvider.AddFiles(@$"{RootPath}\01_foo\foo.bsa", "common/c1", "new1", "new2");
            modIndex.AddFiles("01_foo", "foo.bsa");

            var mod = modRepository.GetById("01");
            // Useful to make sure these DON'T get indexed as _loose_ files.
            Assert.False(modRepository.ContainsFile(mod, "new1", false, false));
            Assert.False(modRepository.ContainsFile(mod, "new2", false, false));
            Assert.True(modRepository.ContainsFile(mod, "new1", true, false));
            Assert.True(modRepository.ContainsFile(mod, "new2", true, false));
            Assert.Collection(
                modRepository.SearchForFiles("common/c1", true, false),
                x => AssertSearchResult(x, "modname1", "foo"),
                x => AssertSearchResult(x, "modname2", "baz"),
                x => AssertSearchResult(x, "modname1", "bar", "bar.bsa"),
                x => AssertSearchResult(x, "modname1", "foo", "foo.bsa"));
            Assert.Collection(
                modRepository.SearchForFiles("new1", true, false),
                x => AssertSearchResult(x, "modname1", "foo", "foo.bsa"));
            Assert.Collection(
                modRepository.SearchForFiles("new2", true, false),
                x => AssertSearchResult(x, "modname1", "foo", "foo.bsa"));
        }

        [Fact]
        public void WhenArchiveDeleted_IndexesNewArchive()
        {
            var mod = modRepository.GetById("01");
            modIndex.RemoveFiles("01_bar", "bar.bsa");

            Assert.Collection(
                modRepository.SearchForFiles("common/c1", true, false),
                x => AssertSearchResult(x, "modname1", "foo"),
                x => AssertSearchResult(x, "modname2", "baz"));
            Assert.Empty(modRepository.SearchForFiles("bar3", true, false));
            Assert.False(modRepository.ContainsFile(mod, "bar3", true, false));
        }

        [Fact]
        public void WhenFileAddedToExistingComponent_IndexesNewFile()
        {
            var mod = modRepository.GetById("02");
            modIndex.AddFiles("02_baz", "new1");

            Assert.True(modRepository.ContainsFile(mod, "new1", false, false));
            Assert.Collection(
                modRepository.SearchForFiles("new1", false, false),
                x => AssertSearchResult(x, "modname2", "baz"));
        }

        [Fact]
        public void WhenFileAddedToNewComponent_IndexesNewFileAndComponent()
        {
            IModLocatorKey modKey = new ModLocatorKey("03", "modname3");
            modNames.Add(modKey.Id, modKey.Name);
            modIndex.AddFiles("03_new", "foo1", "new1", "new2");

            Assert.Contains(modRepository, x =>
                x.Id == "03" &&
                x.Name == "modname3" &&
                x.Components[0].ModKey.Id == "03" &&
                x.Components[0].ModKey.Name == "modname3" &&
                x.Components[0].Id == "new_id" &&
                x.Components[0].Name == "new" &&
                x.Components[0].Path == $@"{RootPath}\03_new" &&
                x.Components[0].IsEnabled);
            var mod = modRepository.GetById("03");
            Assert.NotNull(mod);
            Assert.True(modRepository.ContainsFile(mod, "foo1", false, false));
            Assert.True(modRepository.ContainsFile(mod, "new1", false, false));
            Assert.True(modRepository.ContainsFile(mod, "new2", false, false));
            Assert.Equal(modKey, modRepository.FindByComponentName("new"));
            Assert.Equal(modKey, modRepository.FindByComponentPath($@"{RootPath}\03_new"));
            Assert.Equal(modKey, modRepository.FindByKey(modKey));
            Assert.Equal(modKey, modRepository.GetByName("modname3"));
            Assert.Collection(
                modRepository.SearchForFiles("foo1", false, false),
                x => AssertSearchResult(x, "modname1", "foo"),
                x => AssertSearchResult(x, "modname3", "new"));
            Assert.Collection(
                modRepository.SearchForFiles("new2", false, false),
                x => AssertSearchResult(x, "modname3", "new"));
        }

        [Fact]
        public void WhenFileRemovedFromComponent_DeindexesFile()
        {
            var mod = modRepository.GetById("01");
            modIndex.RemoveFiles("01_foo", "common/c1", "foo1");

            Assert.False(modRepository.ContainsFile(mod, "common/c1", false, false));
            Assert.False(modRepository.ContainsFile(mod, "foo1", false, false));
            Assert.Collection(
                modRepository.SearchForFiles("common/c1", false, false),
                x => AssertSearchResult(x, "modname2", "baz"));
            Assert.Empty(modRepository.SearchForFiles("foo1", false, false));
        }

        [Theory]
        [InlineData("modname1", "common/c1")]
        [InlineData("modname1", "foo1")]
        [InlineData("modname1", "foo2")]
        [InlineData("modname1", "bar1")]
        [InlineData("modname1", "bar2")]
        [InlineData("modname1", "bar3", true)]
        [InlineData("modname1", "bar.bsa")]
        [InlineData("modname2", "common/c1")]
        [InlineData("modname2", "baz1")]
        [InlineData("modname2", "baz2")]
        [InlineData("modname2", "quux1", false, true)]
        [InlineData("modname2", "quux2", false, true)]
        [InlineData("standalone", "foo2")]
        [InlineData("standalone", "bar2")]
        public void WhenFileExists_ContainsFile_ReturnsTrue(
            string modName, string relativePath, bool includeArchives = false, bool includeDisabled = false)
        {
            var mod = modRepository.GetByName(modName);
            Assert.True(modRepository.ContainsFile(mod, relativePath, includeArchives, includeDisabled));
        }

        [Theory]
        [InlineData("modname1", "bar3")]
        [InlineData("modname1", "invalid")]
        [InlineData("modname2", "quux1")]
        [InlineData("modname2", "quux2")]
        public void WhenFileDoesNotExistOrIsFiltered_ContainsFile_ReturnsFalse(
            string modName, string relativePath, bool includeArchives = false, bool includeDisabled = false)
        {
            var mod = modRepository.GetByName(modName);
            Assert.False(modRepository.ContainsFile(mod, relativePath, includeArchives, includeDisabled));
        }

        private static void AssertSearchResult(
            ModSearchResult result, string modName, string componentName, string archiveName = null)
        {
            Assert.Equal(modName, result.ModKey.Name);
            Assert.Equal(componentName, result.ModComponent.Name);
            Assert.Equal(archiveName, result.ArchiveName);
        }
    }
}
