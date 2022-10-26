using System.Runtime.InteropServices;

namespace Focus.Graphics.Formats
{
    public class NativeDdsTextureSource : ITextureSource
    {
        public static NativeDdsTextureSource FromFile(string path)
        {
            return new NativeDdsTextureSource(() => File.ReadAllBytes(path));
        }

        public static Task<NativeDdsTextureSource> PreloadAsync(string path)
        {
            return PreloadAsync(async () => await File.ReadAllBytesAsync(path));
        }

        public static async Task<NativeDdsTextureSource> PreloadAsync(Func<Task<Memory<byte>>> dataSource)
        {
            var data = await dataSource();
            var source = new NativeDdsTextureSource(() => data);
            source.LoadTextureData();
            return source;
        }

        private readonly Func<Memory<byte>> getFileData;

        private int width;
        private int height;
        private int[] bgraValues = Array.Empty<int>();
        private bool isLoaded;

        public TexturePixelFormat Format => TexturePixelFormat.BGRA;

        internal NativeDdsTextureSource(Func<Memory<byte>> getFileData)
        {
            this.getFileData = getFileData;
        }

        public TextureData GetTextureData()
        {
            LoadTextureData();
            return new TextureData((uint)width, (uint)height, bgraValues);
        }

        private unsafe void LoadTextureData()
        {
            if (isLoaded)
                return;
            var bytes = getFileData();
            int hr;
            DDSFile file;
            fixed (byte* dataPtr = bytes.Span)
            {
                hr = NativeMethods.LoadDDSFromMemory(dataPtr, bytes.Length, out file);
            }
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
