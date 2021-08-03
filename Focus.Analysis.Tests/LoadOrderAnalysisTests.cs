using Focus.Analysis.Execution;
using Focus.Analysis.Plugins;
using Focus.Analysis.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Focus.Analysis.Tests
{
    public class LoadOrderAnalysisTests
    {
        private readonly LoadOrderAnalysis analysis;

        public LoadOrderAnalysisTests()
        {
            analysis = new LoadOrderAnalysis
            {
                Plugins = new[]
                {
                    DummyPluginAnalysis(
                        "plugin1",
                        DummyRecordAnalysisGroup(RecordType.Armor, 2, "armor"),
                        DummyRecordAnalysisGroup(RecordType.ArmorAddon, 3, "armorAddon")),
                    DummyPluginAnalysis(
                        "plugin2",
                        DummyRecordAnalysisGroup(RecordType.Armor, 2, "armor"),
                        DummyRecordAnalysisGroup(RecordType.ArmorAddon, 5, "armorAddon")),
                    DummyPluginAnalysis(
                        "plugin3",
                        DummyRecordAnalysisGroup(RecordType.Armor, 1, "armor"),
                        DummyRecordAnalysisGroup(RecordType.Npc, 3, "npc")),
                }
            };
        }

        [Fact]
        public void ExtractChains_ProvidesPluginChainPerRecordType()
        {
            var armorChain = analysis.ExtractChains<FakeRecordAnalysis>(RecordType.Armor);
            var armorAddonChain = analysis.ExtractChains<FakeRecordAnalysis>(RecordType.ArmorAddon);
            var npcChain = analysis.ExtractChains<FakeRecordAnalysis>(RecordType.Npc);

            Assert.Collection(
                armorChain,
                x => x.ChainHas("armor1", "plugin1", "plugin2", "plugin3"),
                x => x.ChainHas("armor2", "plugin1", "plugin2"));
            Assert.Collection(
                armorAddonChain,
                x => x.ChainHas("armorAddon1", "plugin1", "plugin2"),
                x => x.ChainHas("armorAddon2", "plugin1", "plugin2"),
                x => x.ChainHas("armorAddon3", "plugin1", "plugin2"),
                x => x.ChainHas("armorAddon4", "plugin2"),
                x => x.ChainHas("armorAddon5", "plugin2"));
            Assert.Collection(
                npcChain,
                x => x.ChainHas("npc1", "plugin3"),
                x => x.ChainHas("npc2", "plugin3"),
                x => x.ChainHas("npc3", "plugin3"));

        }

        private PluginAnalysis DummyPluginAnalysis(string fileName, params RecordAnalysisGroup[] recordGroups)
        {
            return new PluginAnalysis(fileName)
            {
                Groups = recordGroups.ToDictionary(x => x.Type),
            };
        }

        private RecordAnalysisGroup DummyRecordAnalysisGroup(RecordType type, int count, string editorIdPrefix)
        {
            var records = Enumerable.Range(1, count)
                .Select(i => new FakeRecordAnalysis(type)
                {
                    BasePluginName = "plugin1",
                    LocalFormIdHex = i.ToString("X6"),
                    EditorId = $"{editorIdPrefix}{i}",
                });
            return new RecordAnalysisGroup<FakeRecordAnalysis>(type, records);
        }
    }

    static class ChainAssertions
    {
        public static void ChainHas(
            this RecordAnalysisChain<FakeRecordAnalysis> chain, string editorId, params string[] pluginNames)
        {
            Assert.Equal(editorId, chain.Master.EditorId);
            Assert.Collection(chain, pluginNames
                .Select<string, Action<Sourced<FakeRecordAnalysis>>>(p => x => { Assert.Equal(p, x.PluginName); })
                .ToArray());
        }
    }
}
