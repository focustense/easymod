using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Focus.Environment.Tests
{
    public class LoadOrderGraphTests
    {
        class Plugins
        {
            // These are meant to be perfectly identical to actual Skyrim plugins, but given the complexity of a typical
            // load order, it is likely easier to visualize what's going on with familiar and meaningful names rather
            // than making up dummy names.
            public static readonly PluginInfo Base = new("Skyrim.esm", Enumerable.Empty<string>());
            public static readonly PluginInfo Update = new("Update.esm", new[] { "Skyrim.esm" });
            public static readonly PluginInfo Dawnguard = new("Dawnguard.esm", new[] { "Skyrim.esm", "Update.esm" });
            public static readonly PluginInfo HearthFires = new("HearthFires.esm", new[] { "Skyrim.esm", "Update.esm" });
            public static readonly PluginInfo Dragonborn = new("Dragonborn.esm", new[] { "Skyrim.esm", "Update.esm" });
            public static readonly PluginInfo UnofficialPatch = new(
                "USSEP.esm",
                new[] { "Skyrim.esm", "Update.esm", "Dawnguard.esm", "HearthFires.esm", "Dragonborn.esm" });
            public static readonly PluginInfo VanillaBasedMod = new("VanillaQuest.esp", new[] { "Skyrim.esm" });
            public static readonly PluginInfo UnofficialPatchBasedMod = new(
                "USSEPQuest.esp",
                new[] { "Skyrim.esm", "Update.esm", "Dawnguard.esm", "HearthFires.esm", "Dragonborn.esm", "USSEP.esm" });
            public static readonly PluginInfo PatchForVanillaBasedModAndUnofficialPatchBasedMod = new(
                "VanillaQuest_USSEPQuest_Patch.esp",
                new[] {
                    "Skyrim.esm", "Update.esm", "Dawnguard.esm", "HearthFires.esm", "Dragonborn.esm", "USSEP.esm",
                    "VanillaQuest.esp", "USSEPQuest.esp"
                });
            public static readonly PluginInfo PluginWithUndeclaredMasters = new("Undeclared.esp", new[] { "USSEPQuest.esp" });
        }

        public class Healthy
        {
            readonly LoadOrderGraph graph;

            public Healthy()
            {
                graph = new(new PluginInfo[]
                {
                    Plugins.Base,
                    Plugins.Update,
                    Plugins.Dawnguard,
                    Plugins.HearthFires,
                    Plugins.Dragonborn,
                    Plugins.UnofficialPatch,
                    Plugins.VanillaBasedMod,
                    Plugins.UnofficialPatchBasedMod,
                    Plugins.PatchForVanillaBasedModAndUnofficialPatchBasedMod with { IsEnabled = false },
                }, new HashSet<string>());
            }

            [Fact]
            public void CanLoad_IsTrueForAllPlugins()
            {
                Assert.True(graph.CanLoad("Skyrim.esm"));
                Assert.True(graph.CanLoad("Update.esm"));
                Assert.True(graph.CanLoad("Dawnguard.esm"));
                Assert.True(graph.CanLoad("HearthFires.esm"));
                Assert.True(graph.CanLoad("Dragonborn.esm"));
                Assert.True(graph.CanLoad("USSEP.esm"));
                Assert.True(graph.CanLoad("VanillaQuest.esp"));
                Assert.True(graph.CanLoad("USSEPQuest.esp"));
                Assert.True(graph.CanLoad("VanillaQuest_USSEPQuest_Patch.esp"));
            }

            [Fact]
            public void GetAllMasters_ReturnsDeclaredMasters()
            {
                Assert.Equal(Enumerable.Empty<string>(), graph.GetAllMasters("Skyrim.esm"));
                Assert.Equal(new[] { "Skyrim.esm" }, graph.GetAllMasters("Update.esm"));
                Assert.Equal(new[] { "Skyrim.esm", "Update.esm" }, graph.GetAllMasters("Dawnguard.esm"));
                Assert.Equal(new[] { "Skyrim.esm", "Update.esm" }, graph.GetAllMasters("HearthFires.esm"));
                Assert.Equal(new[] { "Skyrim.esm", "Update.esm" }, graph.GetAllMasters("Dragonborn.esm"));
                Assert.Equal(
                    new[] { "Skyrim.esm", "Update.esm", "Dawnguard.esm", "HearthFires.esm", "Dragonborn.esm" },
                    graph.GetAllMasters("USSEP.esm"));
                Assert.Equal(new[] { "Skyrim.esm" }, graph.GetAllMasters("VanillaQuest.esp"));
                Assert.Equal(
                    new[] { "Skyrim.esm", "Update.esm", "Dawnguard.esm", "HearthFires.esm", "Dragonborn.esm", "USSEP.esm" },
                    graph.GetAllMasters("USSEPQuest.esp"));
                Assert.Equal(
                    new[]
                    {
                        "Skyrim.esm", "Update.esm", "Dawnguard.esm", "HearthFires.esm", "Dragonborn.esm", "USSEP.esm",
                        "VanillaQuest.esp", "USSEPQuest.esp"
                    },
                    graph.GetAllMasters("VanillaQuest_USSEPQuest_Patch.esp"));
            }

            [Fact]
            public void GetAllPluginNames_ReturnsNameForAllNodes()
            {
                Assert.Equal(
                    new[]
                    {
                        "Skyrim.esm", "Update.esm", "Dawnguard.esm", "HearthFires.esm", "Dragonborn.esm", "USSEP.esm",
                        "VanillaQuest.esp", "USSEPQuest.esp", "VanillaQuest_USSEPQuest_Patch.esp",
                    },
                    graph.GetAllPluginNames());
            }

            [Fact]
            public void GetMissingMasters_ReturnsEmptyForAllNodes()
            {
                Assert.Equal(Enumerable.Empty<string>(), graph.GetMissingMasters("Skyrim.esm"));
                Assert.Equal(Enumerable.Empty<string>(), graph.GetMissingMasters("Update.esm"));
                Assert.Equal(Enumerable.Empty<string>(), graph.GetMissingMasters("Dawnguard.esm"));
                Assert.Equal(Enumerable.Empty<string>(), graph.GetMissingMasters("HearthFires.esm"));
                Assert.Equal(Enumerable.Empty<string>(), graph.GetMissingMasters("Dragonborn.esm"));
                Assert.Equal(Enumerable.Empty<string>(), graph.GetMissingMasters("USSEP.esm"));
                Assert.Equal(Enumerable.Empty<string>(), graph.GetMissingMasters("VanillaQuest.esp"));
                Assert.Equal(Enumerable.Empty<string>(), graph.GetMissingMasters("USSEPQuest.esp"));
                Assert.Equal(Enumerable.Empty<string>(), graph.GetMissingMasters("VanillaQuest_USSEPQuest_Patch.esp"));
            }

            [Fact]
            public void IsEnabled_IsTrueForPreviouslyEnabledPlugins()
            {
                Assert.True(graph.IsEnabled("Skyrim.esm"));
                Assert.True(graph.IsEnabled("Update.esm"));
                Assert.True(graph.IsEnabled("Dawnguard.esm"));
                Assert.True(graph.IsEnabled("HearthFires.esm"));
                Assert.True(graph.IsEnabled("Dragonborn.esm"));
                Assert.True(graph.IsEnabled("USSEP.esm"));
                Assert.True(graph.IsEnabled("VanillaQuest.esp"));
                Assert.True(graph.IsEnabled("USSEPQuest.esp"));
                Assert.False(graph.IsEnabled("VanillaQuest_USSEPQuest_Patch.esp"));
            }
        }

        public class WithDisabledPlugin
        {
            readonly LoadOrderGraph graph;

            public WithDisabledPlugin()
            {
                graph = new(new PluginInfo[]
                {
                    Plugins.Base,
                    Plugins.Update,
                    Plugins.Dawnguard,
                    Plugins.HearthFires,
                    Plugins.Dragonborn,
                    Plugins.UnofficialPatch,
                    Plugins.VanillaBasedMod,
                    Plugins.UnofficialPatchBasedMod,
                    Plugins.PatchForVanillaBasedModAndUnofficialPatchBasedMod,
                }, new HashSet<string>());
                graph.SetEnabled("USSEP.esm", false);
            }

            [Fact]
            public void CanLoad_IsFalseForDependentPlugins()
            {
                Assert.True(graph.CanLoad("Skyrim.esm"));
                Assert.True(graph.CanLoad("Update.esm"));
                Assert.True(graph.CanLoad("Dawnguard.esm"));
                Assert.True(graph.CanLoad("HearthFires.esm"));
                Assert.True(graph.CanLoad("Dragonborn.esm"));
                Assert.True(graph.CanLoad("USSEP.esm"));
                Assert.True(graph.CanLoad("VanillaQuest.esp"));
                Assert.False(graph.CanLoad("USSEPQuest.esp"));
                Assert.False(graph.CanLoad("VanillaQuest_USSEPQuest_Patch.esp"));
            }

            [Fact]
            public void GetMissingMasters_ReturnsDisabledMasters()
            {
                Assert.Equal(Enumerable.Empty<string>(), graph.GetMissingMasters("Skyrim.esm"));
                Assert.Equal(Enumerable.Empty<string>(), graph.GetMissingMasters("Update.esm"));
                Assert.Equal(Enumerable.Empty<string>(), graph.GetMissingMasters("Dawnguard.esm"));
                Assert.Equal(Enumerable.Empty<string>(), graph.GetMissingMasters("HearthFires.esm"));
                Assert.Equal(Enumerable.Empty<string>(), graph.GetMissingMasters("Dragonborn.esm"));
                Assert.Equal(Enumerable.Empty<string>(), graph.GetMissingMasters("USSEP.esm"));
                Assert.Equal(Enumerable.Empty<string>(), graph.GetMissingMasters("VanillaQuest.esp"));
                Assert.Equal(new[] { "USSEP.esm" }, graph.GetMissingMasters("USSEPQuest.esp"));
                Assert.Equal(
                    new[] { "USSEP.esm", "USSEPQuest.esp" },
                    graph.GetMissingMasters("VanillaQuest_USSEPQuest_Patch.esp"));
            }

            // Graph deliberately does not propagate the disabled state, so that all implicitly-disabled plugins can be
            // automatically re-enabled if the missing masters are re-enabled.
            // It's the caller's responsibility to combine this with CanLoad.
            [Fact]
            public void IsEnabled_IsFalseForExplicitlyDisabledPlugins()
            {
                Assert.True(graph.IsEnabled("Skyrim.esm"));
                Assert.True(graph.IsEnabled("Update.esm"));
                Assert.True(graph.IsEnabled("Dawnguard.esm"));
                Assert.True(graph.IsEnabled("HearthFires.esm"));
                Assert.True(graph.IsEnabled("Dragonborn.esm"));
                Assert.False(graph.IsEnabled("USSEP.esm"));
                Assert.True(graph.IsEnabled("VanillaQuest.esp"));
                Assert.True(graph.IsEnabled("USSEPQuest.esp"));
                Assert.True(graph.IsEnabled("VanillaQuest_USSEPQuest_Patch.esp"));
            }
        }

        public class WithReenabledPlugin
        {
            readonly LoadOrderGraph graph;

            public WithReenabledPlugin()
            {
                graph = new(new PluginInfo[]
                {
                    Plugins.Base,
                    Plugins.Update,
                    Plugins.Dawnguard,
                    Plugins.HearthFires,
                    Plugins.Dragonborn,
                    Plugins.UnofficialPatch,
                    Plugins.VanillaBasedMod with { IsEnabled = false },
                    Plugins.UnofficialPatchBasedMod,
                    Plugins.PatchForVanillaBasedModAndUnofficialPatchBasedMod with { IsEnabled = false },
                }, new HashSet<string>());
                graph.SetEnabled("Update.esm", false);
                graph.SetEnabled("Update.esm", true);
            }

            [Fact]
            public void CanLoad_IsTrueForDependentPlugins()
            {
                Assert.True(graph.CanLoad("Skyrim.esm"));
                Assert.True(graph.CanLoad("Update.esm"));
                Assert.True(graph.CanLoad("Dawnguard.esm"));
                Assert.True(graph.CanLoad("HearthFires.esm"));
                Assert.True(graph.CanLoad("Dragonborn.esm"));
                Assert.True(graph.CanLoad("USSEP.esm"));
                Assert.True(graph.CanLoad("VanillaQuest.esp"));
                Assert.True(graph.CanLoad("USSEPQuest.esp"));
                Assert.False(graph.CanLoad("VanillaQuest_USSEPQuest_Patch.esp"));
            }

            [Fact]
            public void GetMissingMasters_ExcludesRestoredPlugin()
            {
                Assert.Equal(Enumerable.Empty<string>(), graph.GetMissingMasters("Skyrim.esm"));
                Assert.Equal(Enumerable.Empty<string>(), graph.GetMissingMasters("Update.esm"));
                Assert.Equal(Enumerable.Empty<string>(), graph.GetMissingMasters("Dawnguard.esm"));
                Assert.Equal(Enumerable.Empty<string>(), graph.GetMissingMasters("HearthFires.esm"));
                Assert.Equal(Enumerable.Empty<string>(), graph.GetMissingMasters("Dragonborn.esm"));
                Assert.Equal(Enumerable.Empty<string>(), graph.GetMissingMasters("USSEP.esm"));
                Assert.Equal(Enumerable.Empty<string>(), graph.GetMissingMasters("VanillaQuest.esp"));
                Assert.Equal(Enumerable.Empty<string>(), graph.GetMissingMasters("USSEPQuest.esp"));
                Assert.Equal(new[] { "VanillaQuest.esp" }, graph.GetMissingMasters("VanillaQuest_USSEPQuest_Patch.esp"));
            }

            [Fact]
            public void IsEnabled_IsTrueForPreviouslyEnabledPlugins()
            {
                Assert.True(graph.IsEnabled("Skyrim.esm"));
                Assert.True(graph.IsEnabled("Update.esm"));
                Assert.True(graph.IsEnabled("Dawnguard.esm"));
                Assert.True(graph.IsEnabled("HearthFires.esm"));
                Assert.True(graph.IsEnabled("Dragonborn.esm"));
                Assert.True(graph.IsEnabled("USSEP.esm"));
                Assert.False(graph.IsEnabled("VanillaQuest.esp"));
                Assert.True(graph.IsEnabled("USSEPQuest.esp"));
                Assert.False(graph.IsEnabled("VanillaQuest_USSEPQuest_Patch.esp"));
            }
        }

        public class WithMissingPlugin
        {
            readonly LoadOrderGraph graph;

            public WithMissingPlugin()
            {
                graph = new(new PluginInfo[]
                {
                    Plugins.Base,
                    Plugins.Update,
                    Plugins.Dawnguard,
                    Plugins.HearthFires,
                    // Missing Dragonborn
                    Plugins.UnofficialPatch,
                    Plugins.VanillaBasedMod,
                    Plugins.UnofficialPatchBasedMod,
                    Plugins.PatchForVanillaBasedModAndUnofficialPatchBasedMod,
                }, new HashSet<string>());
            }

            [Fact]
            public void CanLoad_IsFalseForDependentPlugins()
            {
                Assert.True(graph.CanLoad("Skyrim.esm"));
                Assert.True(graph.CanLoad("Update.esm"));
                Assert.True(graph.CanLoad("Dawnguard.esm"));
                Assert.True(graph.CanLoad("HearthFires.esm"));
                Assert.False(graph.CanLoad("USSEP.esm"));
                Assert.True(graph.CanLoad("VanillaQuest.esp"));
                Assert.False(graph.CanLoad("USSEPQuest.esp"));
                Assert.False(graph.CanLoad("VanillaQuest_USSEPQuest_Patch.esp"));
            }

            [Fact]
            public void GetMissingMasters_ReturnsDisabledMasters()
            {
                Assert.Equal(Enumerable.Empty<string>(), graph.GetMissingMasters("Skyrim.esm"));
                Assert.Equal(Enumerable.Empty<string>(), graph.GetMissingMasters("Update.esm"));
                Assert.Equal(Enumerable.Empty<string>(), graph.GetMissingMasters("Dawnguard.esm"));
                Assert.Equal(Enumerable.Empty<string>(), graph.GetMissingMasters("HearthFires.esm"));
                Assert.Equal(Enumerable.Empty<string>(), graph.GetMissingMasters("Dragonborn.esm"));
                Assert.Equal(new[] { "Dragonborn.esm" }, graph.GetMissingMasters("USSEP.esm"));
                Assert.Equal(Enumerable.Empty<string>(), graph.GetMissingMasters("VanillaQuest.esp"));
                Assert.Equal(new[] { "Dragonborn.esm", "USSEP.esm" }, graph.GetMissingMasters("USSEPQuest.esp"));
                Assert.Equal(
                    new[] { "Dragonborn.esm", "USSEP.esm", "USSEPQuest.esp" },
                    graph.GetMissingMasters("VanillaQuest_USSEPQuest_Patch.esp"));
            }
        }

        public class WithBlacklistedPlugins
        {
            readonly LoadOrderGraph graph;

            public WithBlacklistedPlugins()
            {
                graph = new(new PluginInfo[]
                {
                    Plugins.Base,
                    Plugins.Update,
                    Plugins.Dawnguard,
                    Plugins.HearthFires,
                    Plugins.Dragonborn,
                    Plugins.UnofficialPatch,
                    Plugins.VanillaBasedMod,
                    Plugins.UnofficialPatchBasedMod,
                    Plugins.PatchForVanillaBasedModAndUnofficialPatchBasedMod,
                }, new HashSet<string>(new[] { "VanillaQuest.esp", "USSEPQuest.esp" }));
            }

            [Fact]
            public void CanLoad_IsFalseForBlacklistedAndDependentPlugins()
            {
                Assert.True(graph.CanLoad("Skyrim.esm"));
                Assert.True(graph.CanLoad("Update.esm"));
                Assert.True(graph.CanLoad("Dawnguard.esm"));
                Assert.True(graph.CanLoad("Dragonborn.esm"));
                Assert.True(graph.CanLoad("HearthFires.esm"));
                Assert.True(graph.CanLoad("USSEP.esm"));
                Assert.False(graph.CanLoad("VanillaQuest.esp"));
                Assert.False(graph.CanLoad("USSEPQuest.esp"));
                Assert.False(graph.CanLoad("VanillaQuest_USSEPQuest_Patch.esp"));
            }

            [Fact]
            public void GetMissingMasters_ReturnsBlacklistedMasters()
            {
                Assert.Equal(Enumerable.Empty<string>(), graph.GetMissingMasters("Skyrim.esm"));
                Assert.Equal(Enumerable.Empty<string>(), graph.GetMissingMasters("Update.esm"));
                Assert.Equal(Enumerable.Empty<string>(), graph.GetMissingMasters("Dawnguard.esm"));
                Assert.Equal(Enumerable.Empty<string>(), graph.GetMissingMasters("HearthFires.esm"));
                Assert.Equal(Enumerable.Empty<string>(), graph.GetMissingMasters("Dragonborn.esm"));
                Assert.Equal(Enumerable.Empty<string>(), graph.GetMissingMasters("USSEP.esm"));
                Assert.Equal(Enumerable.Empty<string>(), graph.GetMissingMasters("VanillaQuest.esp"));
                Assert.Equal(Enumerable.Empty<string>(), graph.GetMissingMasters("USSEPQuest.esp"));
                Assert.Equal(
                    new[] { "VanillaQuest.esp", "USSEPQuest.esp" },
                    graph.GetMissingMasters("VanillaQuest_USSEPQuest_Patch.esp"));
            }

            // As with prior similar tests, we still preserve "enabled" even if a plugin can't load. Blacklisting doesn't
            // behave any differently here.
            [Fact]
            public void IsEnabled_IsFalseForBlacklistedPlugins()
            {
                Assert.True(graph.IsEnabled("Skyrim.esm"));
                Assert.True(graph.IsEnabled("Update.esm"));
                Assert.True(graph.IsEnabled("Dawnguard.esm"));
                Assert.True(graph.IsEnabled("HearthFires.esm"));
                Assert.True(graph.IsEnabled("Dragonborn.esm"));
                Assert.True(graph.IsEnabled("USSEP.esm"));
                Assert.True(graph.IsEnabled("VanillaQuest.esp"));
                Assert.True(graph.IsEnabled("USSEPQuest.esp"));
                Assert.True(graph.IsEnabled("VanillaQuest_USSEPQuest_Patch.esp"));
            }
        }

        public class WithUndeclaredMastersPlugin
        {
            readonly LoadOrderGraph graph;

            public WithUndeclaredMastersPlugin()
            {
                graph = new(new PluginInfo[]
                {
                    Plugins.Base,
                    Plugins.Update,
                    Plugins.Dawnguard with { IsEnabled = false },
                    Plugins.HearthFires,
                    Plugins.Dragonborn,
                    Plugins.UnofficialPatch,
                    Plugins.UnofficialPatchBasedMod,
                    Plugins.PluginWithUndeclaredMasters,
                }, new HashSet<string>());
            }

            [Fact]
            public void GetAllMasters_Default_ReturnsExplicitMasters()
            {
                Assert.Equal(new[] { "USSEPQuest.esp" }, graph.GetAllMasters("Undeclared.esp"));
            }

            [Fact]
            public void GetAllMasters_Implicit_ReturnsUndeclaredMasters()
            {
                Assert.Equal(
                    new[] {
                        "Skyrim.esm", "Update.esm", "Dawnguard.esm", "HearthFires.esm", "Dragonborn.esm", "USSEP.esm",
                        "USSEPQuest.esp"
                    },
                    graph.GetAllMasters("Undeclared.esp", true));
            }

            [Fact]
            public void GetMissingMasters_IncludesDisabledUndeclaredMasters()
            {
                Assert.Equal(
                    new[] { "Dawnguard.esm", "USSEP.esm", "USSEPQuest.esp" },
                    graph.GetMissingMasters("Undeclared.esp"));
            }
        }
    }
}
