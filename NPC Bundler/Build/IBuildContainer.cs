using System;

namespace Focus.Apps.EasyNpc.Build
{
    public interface IBuildContainer<TKey>
        where TKey : struct
    {
        BuildViewModel<TKey> Build { get; }
    }
}