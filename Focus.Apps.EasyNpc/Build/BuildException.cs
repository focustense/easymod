using System;
using System.Runtime.Serialization;

namespace Focus.Apps.EasyNpc.Build
{
    public class BuildException : Exception
    {
        public BuildException()
        {
        }

        public BuildException(string? message)
            : base(message)
        {
        }

        public BuildException(string? message, Exception? innerException)
            : base(message, innerException)
        {
        }

        protected BuildException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
