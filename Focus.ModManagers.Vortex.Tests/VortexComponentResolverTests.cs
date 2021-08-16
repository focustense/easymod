using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Focus.ModManagers.Vortex.Tests
{
    public class VortexComponentResolverTests
    {
        private const string RootPath = @"C:\Vortex\Staging";

        private readonly ModManifest manifest;
        private readonly VortexComponentResolver resolver;

        public VortexComponentResolverTests()
        {
            manifest = new ModManifest();
            // Set this to make sure it doesn't get used in place of the configured directory.
            manifest.StagingDir = @"D:\Apps\Vortex\Staging";
            // Add some junk to the manifest to make sure it doesn't get used when it shouldn't.
            manifest.Files.Add("foo1", new() { ModId = "foo_mod" });
            manifest.Files.Add("foo2", new() { ModId = "foo_mod" });
            manifest.Files.Add("bar1", new() { ModId = "bar_mod" });
            manifest.Files.Add("bar2", new() { ModId = "bar_mod" });
            manifest.Mods.Add("foo_mod", new() { Name = "Awesome Mod" });
            manifest.Mods.Add("bar_mod", new() { Name = "Immersive Mod" });
            resolver = new VortexComponentResolver(manifest, RootPath);
        }

        [Fact]
        public async Task WhenFileNotInManifest_ResolvesDummyComponent()
        {
            var component = await resolver.ResolveComponentInfo("any");

            Assert.Equal(new ModLocatorKey(string.Empty, "any"), component.ModKey);
            Assert.Equal("any", component.Id);
            Assert.Equal("any", component.Name);
            Assert.Equal(@"C:\Vortex\Staging\any", component.Path);
            Assert.True(component.IsEnabled);
        }

        [Fact]
        public async Task WhenFileInManifest_WithoutModId_ResolvesDummyComponent()
        {
            manifest.Files.Add("emptyfile", new FileInfo());
            var component = await resolver.ResolveComponentInfo("emptyfile");

            Assert.Equal(new ModLocatorKey(string.Empty, "emptyfile"), component.ModKey);
            Assert.Equal("emptyfile", component.Id);
            Assert.Equal("emptyfile", component.Name);
            Assert.Equal(@"C:\Vortex\Staging\emptyfile", component.Path);
            Assert.True(component.IsEnabled);
        }

        [Fact]
        public async Task WhenFileInManifest_WithoutMatchingMod_ResolvesWithModId()
        {
            manifest.Files.Add("modlessfile", new() { ModId = "testmod" });
            var component = await resolver.ResolveComponentInfo("modlessfile");

            Assert.Equal(new ModLocatorKey("testmod", "modlessfile"), component.ModKey);
            Assert.Equal("modlessfile", component.Id);
            Assert.Equal("modlessfile", component.Name);
            Assert.Equal(@"C:\Vortex\Staging\modlessfile", component.Path);
            Assert.True(component.IsEnabled);
        }

        [Fact]
        public async Task WhenFileInManifest_WithMatchingUnnamedMod_ResolvesWithModId()
        {
            manifest.Files.Add("unnamedmodfile", new() { ModId = "testmod" });
            manifest.Mods.Add("testmod", new());
            var component = await resolver.ResolveComponentInfo("unnamedmodfile");

            Assert.Equal(new ModLocatorKey("testmod", "unnamedmodfile"), component.ModKey);
            Assert.Equal("unnamedmodfile", component.Id);
            Assert.Equal("unnamedmodfile", component.Name);
            Assert.Equal(@"C:\Vortex\Staging\unnamedmodfile", component.Path);
            Assert.True(component.IsEnabled);
        }

        [Fact]
        public async Task WhenFileInManifest_WithMatchingNamedMod_ResolvesWithModDetails()
        {
            manifest.Files.Add("namedmodfile", new() { ModId = "testmod" });
            manifest.Mods.Add("testmod", new() { Name = "Best Mod" });
            var component = await resolver.ResolveComponentInfo("namedmodfile");

            Assert.Equal(new ModLocatorKey("testmod", "Best Mod"), component.ModKey);
            Assert.Equal("namedmodfile", component.Id);
            Assert.Equal("namedmodfile", component.Name);
            Assert.Equal(@"C:\Vortex\Staging\namedmodfile", component.Path);
            Assert.True(component.IsEnabled);
        }
    }
}
