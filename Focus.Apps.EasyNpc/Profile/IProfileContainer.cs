using System;

namespace Focus.Apps.EasyNpc.Profile
{
    public interface IProfileContainer<TKey>
        where TKey : struct
    {
        ProfileViewModel<TKey> Profile { get; }
    }
}