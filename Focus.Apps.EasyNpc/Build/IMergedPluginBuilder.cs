﻿using Focus.Apps.EasyNpc.Profile;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace Focus.Apps.EasyNpc.Build
{
    public interface IMergedPluginBuilder<TKey>
        where TKey : struct
    {
        MergedPluginResult Build(
            IReadOnlyList<NpcConfiguration<TKey>> npcs, BuildSettings<TKey> buildSettings, ProgressViewModel progress);
        void CreateDummyPlugin(string fileName);
    }

    public class MergedPluginResult
    {
        public ISet<string> Meshes { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public ISet<string> Morphs { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public ISet<string> Npcs { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public ISet<string> Textures { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public IReadOnlyList<NpcWigConversion> WigConversions { get; init; }
    }

    public class NpcWigConversion
    {
        public string BasePluginName { get; init; }
        public string LocalFormIdHex { get; init; }
        public Color? HairColor { get; init; }
        public IReadOnlyList<HeadPartInfo> AddedHeadParts { get; init; }
        public IReadOnlyList<HeadPartInfo> RemovedHeadParts { get; init; }
    }

    public class HeadPartInfo
    {
        public string EditorId { get; init; }
        public string FileName { get; init; }
    }
}