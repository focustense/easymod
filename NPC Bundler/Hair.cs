using System;
using System.Collections.Generic;

namespace Focus.Apps.EasyNpc
{
    public class Hair<TKey>
    {
        public TKey Key { get; init; }
        public string EditorId { get; init; }
        public string Name { get; init; }
        public string ModelFileName { get; init; }
        public bool IsFemale { get; init; }
        public bool IsMale { get; init; }
        public IReadOnlySet<VanillaRace> ValidRaces { get; init; }
    }
}