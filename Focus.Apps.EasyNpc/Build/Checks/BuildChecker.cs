using Focus.Apps.EasyNpc.Configuration;
using Focus.Apps.EasyNpc.Profiles;
using Focus.Environment;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Focus.Apps.EasyNpc.Build.Checks
{
    public interface IBuildChecker
    {
        PreBuildReport CheckAll(Profile profile, BuildSettings buildSettings);
    }

    public class BuildChecker : IBuildChecker
    {
        private readonly IAppSettings appSettings;
        private readonly IEnumerable<IBuildCheck> checks;
        private readonly IReadOnlyLoadOrderGraph loadOrderGraph;
        private readonly IProfilePolicy profilePolicy;

        public BuildChecker(
            IAppSettings appSettings, IReadOnlyLoadOrderGraph loadOrderGraph, IProfilePolicy profilePolicy,
            IEnumerable<IBuildCheck> checks)
        {
            this.appSettings = appSettings;
            this.checks = checks;
            this.loadOrderGraph = loadOrderGraph;
            this.profilePolicy = profilePolicy;
        }

        public PreBuildReport CheckAll(Profile profile, BuildSettings buildSettings)
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
                        IsLikelyOverhaul = profilePolicy.IsLikelyOverhaul(p),
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
