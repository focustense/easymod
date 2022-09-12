using Mutagen.Bethesda.Plugins.Records;
using System.Diagnostics.CodeAnalysis;

namespace Focus.Tools.EasyFollower
{
    static class MutagenExtensions
    {
        public static bool RemoveByEditorId<T>(
            this IGroup<T> group, string? editorId, [MaybeNullWhen(false)] out T removed)
            where T : class, IMajorRecordGetter
        {
            removed = group
                .Where(x => string.Equals(x.EditorID, editorId, StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();
            if (removed != null)
                group.Remove(removed.FormKey);
            return removed != null;
        }

        public static void ReplaceByEditorId<T>(this IGroup<T> group, ref T record)
            where T : MajorRecord
        {
            if (group.RemoveByEditorId(record.EditorID, out T? removed))
                record = (T)record.Duplicate(removed.FormKey);
            group.Add(record);
        }
    }
}
