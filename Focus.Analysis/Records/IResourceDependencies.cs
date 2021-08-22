using System.Collections.Generic;

namespace Focus.Analysis.Records
{
    public interface IResourceDependencies
    {
        IReadOnlyList<string> UsedMeshes { get; }
        IReadOnlyList<string> UsedTextures { get; }
    }
}
