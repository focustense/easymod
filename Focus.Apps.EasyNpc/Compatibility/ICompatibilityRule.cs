using Serilog.Events;
using System;

namespace Focus.Apps.EasyNpc.Compatibility
{
    public interface ICompatibilityRule<T>
    {
        string Description { get; }
        LogEventLevel LogLevel { get; }
        string Name { get; }

        bool IsSupported(T gameRecord);
    }
}
