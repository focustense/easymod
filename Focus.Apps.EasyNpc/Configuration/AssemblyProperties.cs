using System;
using System.Reflection;

namespace Focus.Apps.EasyNpc.Configuration
{
    public static partial class AssemblyProperties
    {
        private static readonly Version UnknownVersion = new();

        public static string Name => currentAssembly.GetName()?.Name ?? string.Empty;
        public static string Product =>
            currentAssembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product ?? string.Empty;
        public static string Title =>
            currentAssembly.GetCustomAttribute<AssemblyTitleAttribute>()?.Title ?? string.Empty;
        public static Version Version => currentAssembly.GetName().Version ?? UnknownVersion;

        private static readonly Assembly currentAssembly = typeof(AssemblyProperties).Assembly;
    }
}