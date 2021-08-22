using Microsoft.Win32;
using System;

namespace Focus.Abstractions.Windows
{
    public interface IRegistryKeyStatics
    {
        IRegistryKey OpenBaseKey(RegistryHive hive, RegistryView view);
    }

    public interface IRegistryKey : IDisposable
    {
        object? GetValue(string name, object? defaultValue);
        IRegistryKey? OpenSubKey(string name);
    }

    public class RegistryKeyStatics : IRegistryKeyStatics
    {
        public static readonly RegistryKeyStatics Default = new();

        public IRegistryKey OpenBaseKey(RegistryHive hive, RegistryView view)
        {
            return new RegistryKeyWrapper(RegistryKey.OpenBaseKey(hive, view));
        }
    }

    public class RegistryKeyWrapper : IRegistryKey
    {
        private readonly RegistryKey key;

        private bool isDisposed;

        public RegistryKeyWrapper(RegistryKey key)
        {
            this.key = key;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public object? GetValue(string name, object? defaultValue)
        {
            return key.GetValue(name, defaultValue);
        }

        public IRegistryKey? OpenSubKey(string name)
        {
            var subKey = key.OpenSubKey(name);
            return subKey is not null ? new RegistryKeyWrapper(subKey) : null;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (isDisposed)
                return;
            if (disposing)
                key.Dispose();
            isDisposed = true;
        }
    }
}
