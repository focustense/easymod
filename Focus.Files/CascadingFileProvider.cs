using System;
using System.Collections.Generic;
using System.Linq;

namespace Focus.Files
{
    public class CascadingFileProvider : IFileProvider
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

        public ulong GetSize(string fileName)
        {
            return providers
                .Where(p => p.Exists(fileName))
                .Select(p => p.GetSize(fileName))
                .FirstOrDefault();
        }

        public ReadOnlySpan<byte> ReadBytes(string fileName)
        {
            var matchingProvider = providers.FirstOrDefault(p => p.Exists(fileName));
            return matchingProvider != null ? matchingProvider.ReadBytes(fileName) : null;
        }
    }
}