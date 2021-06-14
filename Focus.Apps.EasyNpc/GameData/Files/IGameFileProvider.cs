using System;

namespace Focus.Apps.EasyNpc.GameData.Files
{
    public interface IGameFileProvider : IDisposable
    {
        bool Exists(string fileName);
        string GetPhysicalPath(string fileName);
    }
}