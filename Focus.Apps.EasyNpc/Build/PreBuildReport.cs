#nullable enable

using System;
using System.Collections.Generic;

namespace Focus.Apps.EasyNpc.Build
{
    public class PreBuildReport
    {
        public IReadOnlyList<MasterDependency> Masters { get; init; } = new List<MasterDependency>().AsReadOnly();
        public IReadOnlyList<BuildWarning> Warnings { get; init; } = new List<BuildWarning>().AsReadOnly();

        public class MasterDependency
        {
            public bool IsLikelyOverhaul { get; init; }
            public string PluginName { get; init; } = "";
        }
    }
}