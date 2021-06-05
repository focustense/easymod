using System;

namespace NPC_Bundler
{
    public interface IGameFileProvider : IDisposable
    {
        bool Exists(string fileName);
        string GetPhysicalPath(string fileName);
    }
}