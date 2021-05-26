using System;
using System.Collections.Generic;
using System.Linq;

namespace NPC_Bundler
{
    public class NullExternalLog : IExternalLog
    {
        public IEnumerable<string> GetMessages()
        {
            return Enumerable.Empty<string>();
        }
    }
}