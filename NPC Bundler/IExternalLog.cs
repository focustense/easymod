using System.Collections.Generic;

namespace NPC_Bundler
{
    public interface IExternalLog
    {
        IEnumerable<string> GetMessages();
    }
}