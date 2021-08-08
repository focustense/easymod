using System;
using System.Runtime.Serialization;

namespace Focus.ModManagers
{
    public class ModManagerException : Exception
    {
        public ModManagerException()
        {
        }

        public ModManagerException(string? message)
            : base(message)
        {
        }

        public ModManagerException(string? message, Exception? innerException)
            : base(message, innerException)
        {
        }

        protected ModManagerException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
