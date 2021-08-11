#nullable enable

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Focus.Apps.EasyNpc.Profiles
{
    public interface IMugshotRepository
    {
        // Repository provides only the available mugshots, plus a placeholder mugshot if supported.
        // Combining this with a current mod/plugin list, adding annotations, etc. is the responsibility of the caller.
        Task<IEnumerable<MugshotFile>> GetMugshotFiles(IRecordKey npcKey);
    }
}
