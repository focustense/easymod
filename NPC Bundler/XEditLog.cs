using System;
using System.Collections.Generic;
using System.Linq;
using XeLib.API;

namespace NPC_Bundler
{
    public class XEditLog : IExternalLog
    {
        public IEnumerable<string> GetMessages()
        {
            var newMessages = Messages.GetMessages();
            if (string.IsNullOrEmpty(newMessages))
                return Enumerable.Empty<string>();
            return newMessages
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(msg => $"[XeLib] {msg}");
        }
    }
}