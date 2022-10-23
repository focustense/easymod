using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Focus.Files
{
    public class CascadingFileProvider : IFileProvider, IAsyncFileProvider
    {
        private readonly IEnumerable<IFileProvider> providers;

        public CascadingFileProvider(IEnumerable<IFileProvider> providers)
        {
            this.providers = providers.ToList();
        }

        public bool Exists(string fileName)
        {
            return providers.Any(p => p.Exists(fileName));
        }

        public async Task<bool> ExistsAsync(string fileName)
        {
            var provider = await FindProviderAsync(fileName);
            return provider != null;
        }

        public ulong GetSize(string fileName)
        {
            return providers
                .Where(p => p.Exists(fileName))
                .Select(p => p.GetSize(fileName))
                .FirstOrDefault();
        }

        public async Task<ulong> GetSizeAsync(string fileName)
        {
            var provider = await FindProviderAsync(fileName);
            if (provider == null)
                return default;
            return provider is IAsyncFileProvider asyncProvider
                ? await asyncProvider.GetSizeAsync(fileName) : provider.GetSize(fileName);
        }

        public async Task<Stream> GetStreamAsync(string fileName)
        {
            var provider = await FindProviderAsync(fileName);
            if (provider == null)
                throw new FileNotFoundException(
                    $"No provider found for file {fileName}.", fileName);
            return provider is IAsyncFileProvider asyncProvider
                ? await asyncProvider.GetStreamAsync(fileName)
                : new MemoryStream(provider.ReadBytes(fileName).ToArray());
        }

        public ReadOnlySpan<byte> ReadBytes(string fileName)
        {
            var matchingProvider = providers.FirstOrDefault(p => p.Exists(fileName));
            return matchingProvider != null
                ? matchingProvider.ReadBytes(fileName)
                : throw new FileNotFoundException(
                    $"No provider found for file {fileName}.", fileName);
        }

        public async Task<Memory<byte>> ReadBytesAsync(string fileName)
        {
            var provider = await FindProviderAsync(fileName);
            if (provider == null)
                throw new FileNotFoundException(
                    $"No provider found for file {fileName}.", fileName);
            return provider is IAsyncFileProvider asyncProvider
                ? await asyncProvider.ReadBytesAsync(fileName)
                : provider.ReadBytes(fileName).ToArray();
        }

        private async Task<IFileProvider?> FindProviderAsync(string fileName)
        {
            foreach (var provider in providers)
            {
                if (provider is IAsyncFileProvider asyncProvider)
                {
                    if (await asyncProvider.ExistsAsync(fileName))
                        return provider;
                }
                else if (provider.Exists(fileName))
                    return provider;
            }
            return null;
        }
    }
}