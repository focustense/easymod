using Focus.Apps.EasyNpc.GameData.Files;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace Focus.Apps.EasyNpc.Build
{
    public interface IFaceGenEditor
    {
        void ReplaceHeadParts(
            string faceGenPath, IEnumerable<HeadPartInfo> removedParts, IEnumerable<HeadPartInfo> addedParts,
            Color? hairColor, IArchiveProvider archiveProvider);
    }
}