using Focus.Analysis.Execution;
using Focus.Analysis.Records;
using Focus.Environment;
using Moq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Focus.Analysis.Tests
{
    public class AnalysisRunnerTests
    {
        private readonly List<string> availablePlugins;
        private readonly Mock<IReadOnlyLoadOrderGraph> loadOrderGraphMock;
        private readonly ILogger logger;
        private readonly Mock<IRecordScanner> recordScannerMock;
        private readonly AnalysisRunner runner;

        public AnalysisRunnerTests()
        {
            logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Debug()
                .CreateLogger();
            recordScannerMock = new Mock<IRecordScanner>();
            availablePlugins = new List<string>();
            loadOrderGraphMock = new Mock<IReadOnlyLoadOrderGraph>();
            runner = new AnalysisRunner(recordScannerMock.Object, availablePlugins, loadOrderGraphMock.Object, logger);
        }

        [Fact]
        public void WhenAnalyzerTypeDifferentFromRecordType_Configure_Throws()
        {
            var weaponAnalyzer = new Mock<IRecordAnalyzer<RecordAnalysis>>();
            weaponAnalyzer.SetupGet(x => x.RecordType).Returns(RecordType.Weapon);

            Assert.Throws<ArgumentException>(() => runner.Configure(RecordType.Armor, weaponAnalyzer.Object));
        }

        [Fact]
        public void ForAllPlugins_ReportsMasters()
        {
            availablePlugins.AddRange(new[] { "plugin1", "plugin2", "plugin3" });
            SetupLoadOrderGraph(new()
            {
                { "plugin1", Enumerable.Empty<string>() },
                { "plugin2", new[] { "plugin1" } },
                { "plugin3", new[] { "plugin1" } },
            });
            loadOrderGraphMock.Setup(x => x.GetAllMasters("plugin3", true)).Returns(new[] { "plugin1", "implicit1" });
            var loadOrderAnalysis = runner.Run();

            Assert.Collection(
                loadOrderAnalysis.Plugins,
                plugin =>
                {
                    Assert.Equal("plugin1", plugin.FileName);
                    Assert.Equal(Enumerable.Empty<string>(), plugin.ExplicitMasters);
                    Assert.Equal(Enumerable.Empty<string>(), plugin.ImplicitMasters);
                },
                plugin =>
                {
                    Assert.Equal("plugin2", plugin.FileName);
                    Assert.Equal(new[] { "plugin1" }, plugin.ExplicitMasters);
                    Assert.Equal(new[] { "plugin1" }, plugin.ImplicitMasters);
                },
                plugin =>
                {
                    Assert.Equal("plugin3", plugin.FileName);
                    Assert.Equal(new[] { "plugin1" }, plugin.ExplicitMasters);
                    Assert.Equal(new[] { "plugin1", "implicit1" }, plugin.ImplicitMasters);
                });
        }

        [Fact]
        public void ForNonImplicitPlugins_BaseGame_IsFalse()
        {
            availablePlugins.AddRange(new[] { "plugin1", "plugin2", "plugin3" });
            SetupLoadOrderGraph(new()
            {
                { "plugin1", Enumerable.Empty<string>() },
                { "plugin2", new[] { "plugin1" } },
                { "plugin3", new[] { "plugin1" } },
            });
            loadOrderGraphMock.Setup(x => x.GetAllMasters("plugin3", true)).Returns(new[] { "plugin1", "implicit1" });
            var loadOrderAnalysis = runner.Run();

            Assert.Collection(
                loadOrderAnalysis.Plugins,
                plugin => Assert.False(plugin.IsBaseGame),
                plugin => Assert.False(plugin.IsBaseGame),
                plugin => Assert.False(plugin.IsBaseGame));
        }

        [Fact]
        public void ForAllRecords_RunsConfiguredAnalyzers()
        {
            availablePlugins.AddRange(new[] { "plugin1", "plugin2" });
            SetupLoadOrderGraph(new()
            {
                { "plugin1", Enumerable.Empty<string>() },
                { "plugin2", new[] { "plugin1" } },
            });
            SetupRecordScanner("plugin1", new()
            {
                { RecordType.Book, new[] { "book1:plugin1", "book2:plugin1", "book3:plugin1" } },
                { RecordType.Keyword, new[] { "keyword1:plugin1", "keyword2:plugin1" } },
            });
            SetupRecordScanner("plugin2", new()
            {
                { RecordType.Book, new[] { "book1:plugin1", "book2:plugin1", "book4:plugin2" } },
                { RecordType.Armor, new[] { "armor1:plugin2", "armor2:plugin2" } },
            });

            var loadOrderAnalysis = runner
                .Configure(RecordType.Book, DefaultAnalyzerMock(RecordType.Book, "Book").Object)
                .ConfigureDefault(t => DefaultAnalyzerMock(t, "Default").Object)
                .Run();

            Assert.Collection(
                loadOrderAnalysis.Plugins,
                plugin =>
                {
                    Assert.Equal("plugin1", plugin.FileName);
                    Assert.Equal(Enumerable.Empty<string>(), plugin.ExplicitMasters);
                    Assert.Equal(Enumerable.Empty<string>(), plugin.ImplicitMasters);
                    Assert.Collection(
                        plugin.Groups[RecordType.Book].Records,
                        book =>
                        {
                            Assert.Equal("plugin1", book.BasePluginName);
                            Assert.Equal("book1", book.LocalFormIdHex);
                            Assert.Equal("Book_book1:plugin1", book.EditorId);
                            // The rest of the fields don't really matter. Since we used a unique prefix ("Book") for
                            // the book analyzer, it proves that this particular analyzer ran.
                        },
                        book =>
                        {
                            Assert.Equal("plugin1", book.BasePluginName);
                            Assert.Equal("book2", book.LocalFormIdHex);
                            Assert.Equal("Book_book2:plugin1", book.EditorId);
                        },
                        book =>
                        {
                            Assert.Equal("plugin1", book.BasePluginName);
                            Assert.Equal("book3", book.LocalFormIdHex);
                            Assert.Equal("Book_book3:plugin1", book.EditorId);
                        });
                    Assert.Collection(
                        plugin.Groups[RecordType.Keyword].Records,
                        keyword =>
                        {
                            Assert.Equal("plugin1", keyword.BasePluginName);
                            Assert.Equal("keyword1", keyword.LocalFormIdHex);
                            Assert.Equal("Default_keyword1:plugin1", keyword.EditorId);
                        },
                        keyword =>
                        {
                            Assert.Equal("plugin1", keyword.BasePluginName);
                            Assert.Equal("keyword2", keyword.LocalFormIdHex);
                            Assert.Equal("Default_keyword2:plugin1", keyword.EditorId);
                        });
                },
                plugin =>
                {
                    Assert.Equal("plugin2", plugin.FileName);
                    Assert.Equal(new[] { "plugin1" }, plugin.ExplicitMasters);
                    Assert.Equal(new[] { "plugin1" }, plugin.ImplicitMasters);
                    Assert.Collection(
                        plugin.Groups[RecordType.Book].Records,
                        book =>
                        {
                            Assert.Equal("plugin1", book.BasePluginName);
                            Assert.Equal("book1", book.LocalFormIdHex);
                            Assert.Equal("Book_book1:plugin1", book.EditorId);
                        },
                        book =>
                        {
                            Assert.Equal("plugin1", book.BasePluginName);
                            Assert.Equal("book2", book.LocalFormIdHex);
                            Assert.Equal("Book_book2:plugin1", book.EditorId);
                        },
                        book =>
                        {
                            Assert.Equal("plugin2", book.BasePluginName);
                            Assert.Equal("book4", book.LocalFormIdHex);
                            Assert.Equal("Book_book4:plugin2", book.EditorId);
                        });
                    Assert.Collection(
                        plugin.Groups[RecordType.Armor].Records,
                        armor =>
                        {
                            Assert.Equal("plugin2", armor.BasePluginName);
                            Assert.Equal("armor1", armor.LocalFormIdHex);
                            Assert.Equal("Default_armor1:plugin2", armor.EditorId);
                        },
                        armor =>
                        {
                            Assert.Equal("plugin2", armor.BasePluginName);
                            Assert.Equal("armor2", armor.LocalFormIdHex);
                            Assert.Equal("Default_armor2:plugin2", armor.EditorId);
                        });
                });
        }

        [Fact]
        public void WhenPluginAnalysisFails_ProvidesEmptyAnalysisWithException()
        {
            availablePlugins.AddRange(new[] { "plugin1", "plugin2", "plugin3", "plugin4" });
            SetupLoadOrderGraph(new()
            {
                { "plugin1", Enumerable.Empty<string>() },
                { "plugin2", Enumerable.Empty<string>() },
                { "plugin3", Enumerable.Empty<string>() },
                { "plugin4", Enumerable.Empty<string>() },
            });
            SetupRecordScanner("plugin1", new() { { RecordType.Keyword, new[] { "keyword1:plugin1" } } });
            SetupRecordScanner("plugin2", new() { { RecordType.Keyword, new[] { "keyword2:plugin2" } } });
            SetupRecordScanner("plugin3", new() { { RecordType.Keyword, new[] { "keyword3:plugin3" } } });
            SetupRecordScanner("plugin4", new() { { RecordType.Keyword, new[] { "keyword4:plugin4" } } });
            var analyzerMock = new Mock<IRecordAnalyzer<RecordAnalysis>>();
            analyzerMock.SetupGet(x => x.RecordType).Returns(RecordType.Keyword);
            analyzerMock.Setup(x => x.Analyze(It.IsAny<string>(), It.IsAny<IRecordKey>()))
                .Returns((string pluginName, IRecordKey key) =>
                    pluginName == "plugin1" || pluginName == "plugin4" ?
                        DefaultAnalyze(pluginName, key, RecordType.Keyword, "Keyword") :
                        throw new Exception("Test analysis failure"));

            var loadOrderAnalysis = runner
                .Configure(RecordType.Keyword, analyzerMock.Object)
                .Run();

            Assert.Collection(
                loadOrderAnalysis.Plugins,
                plugin =>
                {
                    Assert.Null(plugin.Exception);
                    Assert.Collection(
                        plugin.Groups[RecordType.Keyword].Records,
                        keyword => Assert.Equal("Keyword_keyword1:plugin1", keyword.EditorId));
                },
                plugin =>
                {
                    Assert.NotNull(plugin.Exception);
                    Assert.Empty(plugin.Groups);
                },
                plugin =>
                {
                    Assert.NotNull(plugin.Exception);
                    Assert.Empty(plugin.Groups);
                },
                plugin =>
                {
                    Assert.Null(plugin.Exception);
                    Assert.Collection(
                        plugin.Groups[RecordType.Keyword].Records,
                        keyword => Assert.Equal("Keyword_keyword4:plugin4", keyword.EditorId));
                });
        }

        [Fact]
        public void WhenBaseNotRequested_IgnoresImplicits()
        {
            availablePlugins.AddRange(new[] { "plugin1", "plugin2", "plugin3" });
            SetupLoadOrderGraph(new()
            {
                { "plugin1", Enumerable.Empty<string>() },
                { "plugin2", Enumerable.Empty<string>() },
                { "plugin3", Enumerable.Empty<string>() },
                { "plugin4", Enumerable.Empty<string>() },
                { "plugin5", Enumerable.Empty<string>() },
            });
            loadOrderGraphMock.Setup(x => x.IsImplicit("plugin1")).Returns(true);
            loadOrderGraphMock.Setup(x => x.IsImplicit("plugin2")).Returns(true);
            var loadOrderAnalysis = runner.Run();

            Assert.DoesNotContain(loadOrderAnalysis.Plugins, p => p.FileName == "plugin1");
            Assert.DoesNotContain(loadOrderAnalysis.Plugins, p => p.FileName == "plugin2");
        }

        [Fact]
        public void WhenBaseRequested_IncludesImplicits()
        {
            availablePlugins.AddRange(new[] { "plugin1", "plugin2", "plugin3" });
            SetupLoadOrderGraph(new()
            {
                { "plugin1", Enumerable.Empty<string>() },
                { "plugin2", Enumerable.Empty<string>() },
                { "plugin3", Enumerable.Empty<string>() },
                { "plugin4", Enumerable.Empty<string>() },
                { "plugin5", Enumerable.Empty<string>() },
            });
            loadOrderGraphMock.Setup(x => x.IsImplicit("plugin1")).Returns(true);
            loadOrderGraphMock.Setup(x => x.IsImplicit("plugin2")).Returns(true);
            var loadOrderAnalysis = runner.Run(includeBasePlugins: true);

            Assert.Contains(loadOrderAnalysis.Plugins, p => p.FileName == "plugin1");
            Assert.Contains(loadOrderAnalysis.Plugins, p => p.FileName == "plugin2");
            Assert.True(loadOrderAnalysis.Plugins[0].IsBaseGame);
            Assert.True(loadOrderAnalysis.Plugins[1].IsBaseGame);
        }

        [Fact]
        public void WhenRecordTypesIgnored_ExcludesFromAnalysis()
        {
            availablePlugins.AddRange(new[] { "plugin1", "plugin2", "plugin3" });
            SetupLoadOrderGraph(new()
            {
                { "plugin1", Enumerable.Empty<string>() },
                { "plugin2", new[] { "plugin1" } },
                { "plugin3", new[] { "plugin1" } },
            });
            SetupRecordScanner("plugin1", new()
            {
                { RecordType.Armor, new[] { "armor1:plugin1" } },
                { RecordType.Weapon, new[] { "weapon1:plugin1" } },
                { RecordType.Water, new[] { "water1:plugin1" } },
            });
            SetupRecordScanner("plugin2", new()
            {
                { RecordType.Armor, new[] { "armor1:plugin1", "armor2:plugin2" } },
                { RecordType.Weapon, new[] { "weapon1:plugin1", "weapon2:plugin2" } },
            });
            SetupRecordScanner("plugin3", new()
            {
                { RecordType.Armor, new[] { "armor1:plugin1", "armor2:plugin2", "armor3:plugin3" } },
                { RecordType.Water, new[] { "water1:plugin1", "water3:plugin3" } },
            });

            var loadOrderAnalysis = runner
                .Ignore(RecordType.Water)
                .ConfigureDefault(t => DefaultAnalyzerMock(t, "Default").Object)
                .Run();

            var includedRecordTypes = loadOrderAnalysis.Plugins
                .SelectMany(x => x.Groups)
                .Where(x => x.Value.Records.Count > 0)
                .Select(x => x.Key)
                .Distinct();
            var includedEditorIds = loadOrderAnalysis.Plugins
                .SelectMany(x => x.Groups)
                .SelectMany(x => x.Value.Records)
                .Select(x => x.EditorId)
                .Distinct();
            Assert.Equal(new[] { RecordType.Armor, RecordType.Weapon }, includedRecordTypes);
            Assert.Equal(new[]
            {
                "Default_armor1:plugin1",
                "Default_weapon1:plugin1",
                "Default_armor2:plugin2",
                "Default_weapon2:plugin2",
                "Default_armor3:plugin3"
            }, includedEditorIds);
        }

        private FakeRecordAnalysis DefaultAnalyze(
            string pluginName, IRecordKey key, RecordType recordType, string editorIdPrefix)
        {
            return new FakeRecordAnalysis(recordType)
            {
                BasePluginName = key.BasePluginName,
                LocalFormIdHex = key.LocalFormIdHex,
                EditorId = $"{editorIdPrefix}_{key}",
                Exists = true,
                IsInjectedOrInvalid = false,
                IsOverride = !key.PluginEquals(pluginName),
            };
        }

        private Mock<IRecordAnalyzer<RecordAnalysis>> DefaultAnalyzerMock(RecordType recordType, string editorIdPrefix)
        {
            var mock = new Mock<IRecordAnalyzer<RecordAnalysis>>();
            mock.SetupGet(x => x.RecordType).Returns(recordType);
            mock.Setup(x => x.Analyze(It.IsAny<string>(), It.IsAny<IRecordKey>()))
                .Returns((string pluginName, IRecordKey key) =>
                    DefaultAnalyze(pluginName, key, recordType, editorIdPrefix));
            return mock;
        }

        private void SetupLoadOrderGraph(Dictionary<string, IEnumerable<string>> plugins)
        {
            loadOrderGraphMock.Setup(x => x.CanLoad(It.IsAny<string>())).Returns(true);
            loadOrderGraphMock.Setup(x => x.IsEnabled(It.IsAny<string>())).Returns(false);
            loadOrderGraphMock.Setup(x => x.GetMissingMasters(It.IsAny<string>())).Returns(Enumerable.Empty<string>());
            foreach (var plugin in plugins)
            {
                loadOrderGraphMock.Setup(x => x.GetAllMasters(plugin.Key, It.IsAny<bool>())).Returns(plugin.Value);
                loadOrderGraphMock.Setup(x => x.IsEnabled(It.IsAny<string>())).Returns(true);
            }
        }

        private void SetupRecordScanner(string pluginName, Dictionary<RecordType, IEnumerable<string>> recordKeys)
        {
            foreach (var item in recordKeys)
            {
                recordScannerMock.Setup(x => x.GetKeys(pluginName, item.Key))
                    .Returns(item.Value.Select(s => RecordKey.Parse(s)));
            }
        }
    }
}
