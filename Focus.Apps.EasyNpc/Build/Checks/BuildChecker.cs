using Focus.Apps.EasyNpc.Configuration;
using Focus.Environment;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Focus.Apps.EasyNpc.Build.Checks
{
    public interface IBuildChecker
    {
        PreBuildReport CheckAll(Profiles.Profile profile, BuildSettings buildSettings);
    }

    public class BuildChecker : IBuildChecker
    {
        private readonly IAppSettings appSettings;
        private readonly IEnumerable<IBuildCheck> checks;
        private readonly IReadOnlyLoadOrderGraph loadOrderGraph;

        public BuildChecker(
            IAppSettings appSettings, IReadOnlyLoadOrderGraph loadOrderGraph, IEnumerable<IBuildCheck> checks)
        {
            this.appSettings = appSettings;
            this.checks = checks;
            this.loadOrderGraph = loadOrderGraph;
        }

        public PreBuildReport CheckAll(Profiles.Profile profile, BuildSettings buildSettings)
        {
            var warnings = checks.AsParallel().SelectMany(x => x.Run(profile, buildSettings));
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
                        // TODO: Figure out how to plumb this junk
                        // IsLikelyOverhaul = profileRuleSet.IsLikelyOverhaul(p),
                        IsLikelyOverhaul = false,
                    })
                    .ToList()
                    .AsReadOnly(),
                Warnings = warnings
                    .Where(x =>
                        string.IsNullOrEmpty(x.PluginName) ||
                        x.Id == null ||
                        !suppressions[x.PluginName].Contains((BuildWarningId)x.Id))
                    .OrderBy(x => x.Id)
                    .ThenBy(x => x.PluginName)
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
