using System;
using System.Collections.Generic;

namespace NPC_Bundler
{
    public interface IFaceGenEditor
    {
        void ReplaceHeadParts(
            string faceGenPath, IEnumerable<HeadPartInfo> removedParts, IEnumerable<HeadPartInfo> addedParts,
            IArchiveProvider archiveProvider);
    }
}