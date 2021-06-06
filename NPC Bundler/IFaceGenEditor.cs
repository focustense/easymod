using System;
using System.Collections.Generic;
using System.Drawing;

namespace NPC_Bundler
{
    public interface IFaceGenEditor
    {
        void ReplaceHeadParts(
            string faceGenPath, IEnumerable<HeadPartInfo> removedParts, IEnumerable<HeadPartInfo> addedParts,
            Color? hairColor, IArchiveProvider archiveProvider);
    }
}