using Focus.Apps.EasyNpc.Configuration;
using Focus.Apps.EasyNpc.Profiles;
using Focus.Environment;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Focus.Apps.EasyNpc.Build.Checks
{
    public interface IBuildChecker
    {
        PreBuildReport CheckAll(Profile profile, BuildSettings buildSettings);
    }

    public class BuildChecker : IBuildChecker
    {
        private readonly IAppSettings appSettings;
        private readonly IGameSettings gameSettings;
        private readonly IEnumerable<IGlobalBuildCheck> globalChecks;
        private readonly IReadOnlyLoadOrderGraph loadOrderGraph;
        private readonly IEnumerable<INpcBuildCheck> npcChecks;
        private readonly IProfilePolicy profilePolicy;

        public BuildChecker(
            IAppSettings appSettings, IGameSettings gameSettings, IReadOnlyLoadOrderGraph loadOrderGraph,
            IProfilePolicy profilePolicy, IEnumerable<IGlobalBuildCheck> globalChecks,
            IEnumerable<INpcBuildCheck> npcChecks)
        {
            this.appSettings = appSettings;
            this.gameSettings = gameSettings;
            this.globalChecks = globalChecks;
            this.loadOrderGraph = loadOrderGraph;
            this.npcChecks = npcChecks;
            this.profilePolicy = profilePolicy;
        }

        public PreBuildReport CheckAll(Profile profile, BuildSettings buildSettings)
        {
            Parallel.ForEach(npcChecks.OfType<IPreparableNpcBuildCheck>(), check => check.Prepare(profile));
            var globalWarnings = globalChecks.AsParallel().SelectMany(x => x.Run(profile, buildSettings));
            var npcWarnings = npcChecks.AsParallel()
                .SelectMany(c => profile.Npcs.SelectMany(x => c.Run(x, buildSettings)));
            var suppressions = GetBuildWarningSuppressions();
            var defaultPluginNames = profile.Npcs
                .Select(x => x.DefaultOption.PluginName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var masterPluginNames = defaultPluginNames
                .SelectMany(p => loadOrderGraph.GetAllMasters(p))
                .Concat(defaultPluginNames)
                .Distinct(StringComparer.OrdinalIgnoreCase);
            return new()
            {
                Masters = masterPluginNames
                    .Select(p => new PreBuildReport.MasterDependency
                    {
                        PluginName = p,
                        IsLikelyOverhaul = profilePolicy.IsLikelyOverhaul(p),
                    })
                    .ToList()
                    .AsReadOnly(),
                Warnings = globalWarnings.Concat(npcWarnings)
                    .Where(x =>
                        string.IsNullOrEmpty(x.PluginName) ||
                        x.Id == null ||
                        !suppressions[x.PluginName].Contains((BuildWarningId)x.Id))
                    .AsEnumerable()
                    .OrderBy(x => x.Id)
                    .ThenBy(x => x.PluginName, StringComparer.CurrentCultureIgnoreCase)
                    .ThenByLoadOrder(x => x.RecordKey ?? RecordKey.Null, gameSettings.PluginLoadOrder)
                    .ToList()
                    .AsReadOnly()
            };
        }

        private ILookup<string, BuildWarningId> GetBuildWarningSuppressions()
        {
            return appSettings.BuildWarningWhitelist
                .SelectMany(x => x.IgnoredWarnings.Select(id => new { Plugin = x.PluginName, Id = id }))
                .ToLookup(x => x.Plugin, x => x.Id);
        }
    }
}
