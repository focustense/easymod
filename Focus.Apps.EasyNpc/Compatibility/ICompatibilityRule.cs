using System;

namespace Focus.Apps.EasyNpc.Compatibility
{
    public interface ICompatibilityRule<T>
    {
        string Description { get; }
        string Name { get; }

        bool IsSupported(T gameRecord);
    }
}
