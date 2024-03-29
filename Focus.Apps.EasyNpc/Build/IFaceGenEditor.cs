﻿using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;

namespace Focus.Apps.EasyNpc.Build
{
    public interface IFaceGenEditor
    {
        IEnumerable<string> GetHeadPartNames(string referencePath, byte[] faceGenData);
        Task<IEnumerable<string>> GetHeadPartNames(string faceGenPath);
        Task<bool> ReplaceHeadParts(
            string faceGenPath, IEnumerable<HeadPartInfo> removedParts, IEnumerable<HeadPartInfo> addedParts,
            Color? hairColorNullable);
    }
}