using System;

namespace NPC_Bundler
{
    public class BuildSettings<TKey>
    {
        public bool EnableDewiggify { get; init; }
        public string OutputDirectory { get; init; }
        public string OutputModName { get; init; }

        public IWigResolver<TKey> WigResolver { get; init; }
    }
}