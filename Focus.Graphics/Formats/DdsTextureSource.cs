using System.Runtime.InteropServices;

namespace Focus.Graphics.Formats
{
    public class DdsTextureSource : ITextureSource
    {
        public static DdsTextureSource FromFile(string path)
        {
            return new DdsTextureSource(() => File.ReadAllBytes(path));
        }

        public static async Task<DdsTextureSource> PreloadAsync(string path)
        {
            var data = await File.ReadAllBytesAsync(path);
            var source = new DdsTextureSource(() => data);
            source.LoadTextureData();
            return source;
        }

        private readonly Func<byte[]> getFileData;

        private int width;
        private int height;
        private int[] bgraValues = Array.Empty<int>();
        private bool isLoaded;

        public TexturePixelFormat Format => TexturePixelFormat.BGRA;

        internal DdsTextureSource(Func<byte[]> getFileData)
        {
            this.getFileData = getFileData;
        }

        public TextureData GetTextureData()
        {
            LoadTextureData();
            return new TextureData((uint)width, (uint)height, bgraValues);
        }

        private void LoadTextureData()
        {
            if (isLoaded)
                return;
            var bytes = getFileData();
            var hr = NativeMethods.LoadDDSFromMemory(bytes, bytes.Length, out var file);
            if (hr < 0)
                Marshal.ThrowExceptionForHR(hr);
            try
            {
                width = file.Width;
                height = file.Height;
                bgraValues = new int[file.Width * file.Height];
                for (var row = 0; row < file.Height; row++)
                    Marshal.Copy(
                        file.Scan0 + row * file.Stride,
                        bgraValues,
                        row * file.Width,
                        file.Width);
            }
            finally
            {
                NativeMethods.FreeDDS(ref file);
            }
            isLoaded = true;
        }
    }
}
