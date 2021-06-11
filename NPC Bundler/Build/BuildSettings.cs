using System;

namespace Focus.Apps.EasyNpc.Build
{
    public class BuildSettings<TKey>
    {
        public bool EnableDewiggify { get; init; }
        public string OutputDirectory { get; init; }
        public string OutputModName { get; init; }

        public IWigResolver<TKey> WigResolver { get; init; }
    }
}