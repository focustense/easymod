using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
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
        public static FormKey ToFormKey(this IRecordKey recordKey)
        {
            return FormKey.Factory($"{recordKey.LocalFormIdHex}:{recordKey.BasePluginName}");
        }

        public static RecordKey ToRecordKey(this FormKey formKey)
        {
            return new RecordKey(formKey.ModKey.FileName, formKey.IDString());
        }

    }
}
