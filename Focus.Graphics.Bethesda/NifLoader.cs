using Focus.Files;
using nifly;

namespace Focus.Graphics.Bethesda
{
    public delegate Task<NifFile> FileLoaderAsync();

    public static class NifLoader
    {
        public static FileLoaderAsync FromProviderFile(IAsyncFileProvider provider, string fileName)
        {
            return async () =>
            {
                var data = await provider.ReadBytesAsync(fileName);
                return new NifFile(new vectoruchar(data.ToArray()));
            };
        }
    }
}
