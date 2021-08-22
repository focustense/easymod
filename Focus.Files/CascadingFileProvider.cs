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

        public ReadOnlySpan<byte> ReadBytes(string fileName)
        {
            var matchingProvider = providers.FirstOrDefault(p => p.Exists(fileName));
            return matchingProvider != null ? matchingProvider.ReadBytes(fileName) : null;
        }
    }
}