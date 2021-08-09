using System;

namespace Focus.Abstractions.Windows
{
    public interface IEnvironmentStatics
    {
        string GetFolderPath(Environment.SpecialFolder folder);
    }

    public class EnvironmentStatics : IEnvironmentStatics
    {
        public static readonly EnvironmentStatics Default = new();

        public string GetFolderPath(Environment.SpecialFolder folder)
        {
            return Environment.GetFolderPath(folder);
        }
    }
}
