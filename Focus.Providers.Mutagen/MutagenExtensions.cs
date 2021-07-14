using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins.Records;
using System;
using System.IO;

namespace Focus.Providers.Mutagen
{
    public static class MutagenExtensions
    {
        public static string GetRealDataDirectory<TModSetter, TModGetter>(this GameEnvironmentState<TModSetter, TModGetter> env)
            where TModSetter : class, IContextMod<TModSetter, TModGetter>, TModGetter
            where TModGetter : class, IContextGetterMod<TModSetter, TModGetter>
        {
            var leafDirectory = new DirectoryInfo(env.DataFolderPath).Name;
            return leafDirectory.Equals("data", StringComparison.OrdinalIgnoreCase) ?
                env.DataFolderPath : Path.Combine(env.DataFolderPath, "data");

        }
    }
}
