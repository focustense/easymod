using Mutagen.Bethesda.Plugins;
using System;
using System.Runtime.Serialization;
using System.Text;

namespace Focus.Apps.EasyNpc.Mutagen
{
    public class MissingRecordException : Exception
    {
        public FormKey? FormKey { get; private init; }
        public string? ModName { get; private init; }
        public string? RecordType { get; private init; }

        public MissingRecordException()
        {
        }

        public MissingRecordException(string message)
            : base(message)
        {
        }

        public MissingRecordException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public MissingRecordException(FormKey formKey)
            : base(GetExceptionMessage(formKey))
        {
            FormKey = formKey;
        }

        public MissingRecordException(FormKey formKey, string recordType)
            : base(GetExceptionMessage(formKey, recordType))
        {
            FormKey = formKey;
            RecordType = recordType;
        }

        public MissingRecordException(FormKey formKey, string recordType, string modName)
            : base(GetExceptionMessage(formKey, recordType, modName))
        {
            FormKey = formKey;
            RecordType = recordType;
            ModName = modName;
        }

        protected MissingRecordException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        private static string GetExceptionMessage(FormKey formKey, string? recordType = null, string? modName = null)
        {
            var sb = new StringBuilder($"Could not locate record {formKey}");
            if (!string.IsNullOrEmpty(recordType))
                sb.Append($" of type {recordType}");
            if (!string.IsNullOrEmpty(modName))
                sb.Append($" in plugin {modName}");
            return sb.Append('.').ToString();
        }
    }
}