using System.Collections.Generic;

namespace Focus.Apps.EasyNpc
{
    public interface IExternalLog
    {
        IEnumerable<string> GetMessages();
    }
}