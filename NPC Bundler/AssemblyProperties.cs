using System;
using System.Reflection;

namespace Focus.Apps.EasyNpc
{
    public static class AssemblyProperties
    {
        public static string Name => currentAssembly.GetName().Name;
        public static string Product => currentAssembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product;
        public static string Title => currentAssembly.GetCustomAttribute<AssemblyTitleAttribute>()?.Title;
        public static Version Version => currentAssembly.GetName().Version;

        private static readonly Assembly currentAssembly = typeof(AssemblyProperties).Assembly;
    }
}