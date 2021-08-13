using Focus.Files;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Focus.ModManagers.Tests
{
    public class ComponentPerDirectoryModRepositoryTests
    {
        class DummyComponentResolver : IComponentResolver
        {
            private readonly string rootPath;

            public DummyComponentResolver(string rootPath)
            {
                this.rootPath = rootPath;
            }

            public Task<ModComponentInfo> ResolveComponentInfo(string componentName)
            {
                throw new NotImplementedException();
            }

            public override string ToString()
            {
                return $"Resolver:{rootPath}";
            }
        }

        class TestRepository : ComponentPerDirectoryModRepository<ComponentPerDirectoryConfiguration>
        {
            public TestRepository(IFileSystem fs, IIndexedModRepository inner)
                : base(fs, inner)
            {
            }

            protected override Task<INotifyingBucketedFileIndex> BuildIndexAsync(
                ComponentPerDirectoryConfiguration config)
            {
                var indexMock = new Mock<INotifyingBucketedFileIndex>();
                // We don't care about a thing with regard to what this index actually does; it's just passed through to
                // the base class. All we need to know is that it was created with the right config.
                indexMock.Setup(x => x.ToString()).Returns($"Index:{config.RootPath}");
                return Task.FromResult(indexMock.Object);
            }

            protected override IComponentResolver GetComponentResolver(ComponentPerDirectoryConfiguration config)
            {
                return new DummyComponentResolver(config.RootPath);
            }
        }

        private static readonly ModInfo DummyMod = new("dummyId", "dummyName");

        private readonly MockFileSystem fs;
        private readonly Mock<IIndexedModRepository> indexedMock;
        private readonly ComponentPerDirectoryModRepository<ComponentPerDirectoryConfiguration> repository;

        public ComponentPerDirectoryModRepositoryTests()
        {
            fs = new MockFileSystem();
            indexedMock = new Mock<IIndexedModRepository>();
            indexedMock.Setup(x => x.FindByComponentName(It.IsAny<string>())).Returns(DummyMod);
            indexedMock.Setup(x => x.FindByComponentPath(It.IsAny<string>())).Returns(DummyMod);
            indexedMock.Setup(x => x.FindByKey(It.IsAny<IModLocatorKey>())).Returns(DummyMod);
            indexedMock.Setup(x => x.GetById(It.IsAny<string>())).Returns(DummyMod);
            indexedMock.Setup(x => x.GetByName(It.IsAny<string>())).Returns(DummyMod);
            indexedMock.Setup(x => x.GetEnumerator()).Returns(new List<ModInfo> { DummyMod }.GetEnumerator());
            repository = new TestRepository(fs, indexedMock.Object);
        }

        [Fact]
        public async Task Configure_CreatesNewResolverAndConfiguresInnerIndex()
        {
            await repository.Configure(new ComponentPerDirectoryConfiguration(@"D:\new\root\path"));

            indexedMock.Verify(
                x => x.ConfigureIndex(
                    It.Is<INotifyingBucketedFileIndex>(x => x.ToString() == @"Index:D:\new\root\path"),
                    @"D:\new\root\path",
                    It.Is<IComponentResolver>(x => x.ToString() == @"Resolver:D:\new\root\path")),
                Times.Once());
        }

        [Theory]
        [InlineData(@"path\to\file", false, false)]
        [InlineData(@"path\to\file", true, false)]
        [InlineData(@"path\to\file", false, true)]
        [InlineData(@"path\to\file", true, true)]
        public void ContainsFile_ForComponents_Passthrough(
            string relativePath, bool includeArchives, bool includeDisabled)
        {
            var components = new[] { new ModComponentInfo(ModLocatorKey.Empty, "cid", "cname", relativePath) };
            repository.ContainsFile(components, relativePath, includeArchives, includeDisabled);
            indexedMock.Verify(
                x => x.ContainsFile(components, relativePath, includeArchives, includeDisabled), Times.Once());
        }

        [Theory]
        [InlineData("123", @"path\to\file", false, false)]
        [InlineData("123", @"path\to\file", true, false)]
        [InlineData("123", @"path\to\file", false, true)]
        [InlineData("123", @"path\to\file", true, true)]
        public void ContainsFile_ForMod_Passthrough(
            string modId, string relativePath, bool includeArchives, bool includeDisabled)
        {
            var modInfo = new ModInfo(modId, "");
            repository.ContainsFile(modInfo, relativePath, includeArchives, includeDisabled);
            indexedMock.Verify(
                x => x.ContainsFile(modInfo, relativePath, includeArchives, includeDisabled), Times.Once());
        }

        [Fact]
        public void FindByComponentName_Passthrough()
        {
            var mod = repository.FindByComponentName("aaa");
            indexedMock.Verify(x => x.FindByComponentName("aaa"), Times.Once());
            Assert.Same(DummyMod, mod);
        }

        [Fact]
        public void FindByComponentPath_Passthrough()
        {
            var mod = repository.FindByComponentPath("somepath");
            indexedMock.Verify(x => x.FindByComponentPath("somepath"), Times.Once());
            Assert.Same(DummyMod, mod);
        }

        [Fact]
        public void FindByKey_Passthrough()
        {
            var key = new ModLocatorKey("searchId", "searchName");
            var mod = repository.FindByKey(key);
            indexedMock.Verify(x => x.FindByKey(key), Times.Once());
            Assert.Same(DummyMod, mod);
        }

        [Fact]
        public void GetById()
        {
            var mod = repository.GetById("bbb");
            indexedMock.Verify(x => x.GetById("bbb"), Times.Once());
            Assert.Same(DummyMod, mod);
        }

        [Fact]
        public void GetByName_Passthrough()
        {
            var mod = repository.GetByName("modname");
            indexedMock.Verify(x => x.GetByName("modname"), Times.Once());
            Assert.Same(DummyMod, mod);
        }

        [Fact]
        public void GetEnumerator_Passthrough()
        {
            var mods = repository.ToList();
            indexedMock.Verify(x => x.GetEnumerator(), Times.Once());
            Assert.Equal(new[] { DummyMod }, mods);
        }

        [Theory]
        [InlineData(@"path\to\file", false, false)]
        [InlineData(@"path\to\file", true, false)]
        [InlineData(@"path\to\file", false, true)]
        [InlineData(@"path\to\file", true, true)]
        public void SearchForFiles_Passthrough(string relativePath, bool includeArchives, bool includeDisabled)
        {
            var dummyComponent = new ModComponentInfo(ModLocatorKey.Empty, "", "", "");
            var innerResults = new ModSearchResult[] { new ModSearchResult(dummyComponent, relativePath, null) };
            indexedMock.Setup(x => x.SearchForFiles(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
                .Returns(innerResults);
            var results = repository.SearchForFiles(relativePath, includeArchives, includeDisabled);

            Assert.Equal(innerResults, results);
            indexedMock.Verify(x => x.SearchForFiles(relativePath, includeArchives, includeDisabled), Times.Once());
        }
    }
}
