using System.Collections.Generic;

namespace Focus.Apps.EasyNpc.Debug
{
    public interface IExternalLog
    {
        IEnumerable<string> GetMessages();
    }
}