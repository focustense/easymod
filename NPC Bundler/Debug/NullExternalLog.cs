using System;
using System.Collections.Generic;
using System.Linq;

namespace Focus.Apps.EasyNpc.Debug
{
    public class NullExternalLog : IExternalLog
    {
        public IEnumerable<string> GetMessages()
        {
            return Enumerable.Empty<string>();
        }
    }
}