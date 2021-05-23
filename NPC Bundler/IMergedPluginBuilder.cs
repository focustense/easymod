using System;
using System.Collections.Generic;

namespace NPC_Bundler
{
    public interface IMergedPluginBuilder<TKey>
        where TKey : struct
    {
        MergedPluginResult Build(
            IReadOnlyList<NpcConfiguration<TKey>> npcs, string outputModName, ProgressViewModel progress);
    }

    public class MergedPluginResult
    {
        public ISet<string> Meshes { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public ISet<string> Morphs { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public ISet<string> Npcs { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public ISet<string> Textures { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }
}