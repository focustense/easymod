using System;

namespace Focus.Apps.EasyNpc
{
    public interface IGameFileProvider : IDisposable
    {
        bool Exists(string fileName);
        string GetPhysicalPath(string fileName);
    }
}