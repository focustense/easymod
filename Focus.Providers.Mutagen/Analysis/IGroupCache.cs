using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Noggog;
using System;
using RecordType = Focus.Analysis.Records.RecordType;

namespace Focus.Providers.Mutagen.Analysis
{
    public interface IGroupCache
    {
        IReadOnlyCache<ISkyrimMajorRecordGetter, FormKey>? Get(string pluginName, RecordType recordType);
        IGroupGetter<T>? Get<T>(string pluginName, Func<ISkyrimModGetter, IGroupGetter<T>> groupSelector)
            where T : class, ISkyrimMajorRecordGetter;
        ISkyrimModGetter? GetMod(string pluginName);
        bool MasterExists(FormKey formKey, RecordType recordType);
    }
}