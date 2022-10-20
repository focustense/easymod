using System.Runtime.InteropServices;

namespace Focus.Graphics.Formats
{
    public class DdsTextureSource : ITextureSource
    {
        public static DdsTextureSource FromFile(string path)
        {
            return new DdsTextureSource(() => File.ReadAllBytes(path));
        }

        private readonly Func<byte[]> loadData;

        public TexturePixelFormat Format => TexturePixelFormat.BGRA;

        internal DdsTextureSource(Func<byte[]> loadData)
        {
            this.loadData = loadData;
        }

        public TextureData GetTextureData()
        {
            var bytes = loadData();
            var file = new DDSFile();
            var hr = NativeMethods.LoadDDSFromMemory(bytes, bytes.Length, ref file);
            if (hr < 0)
                Marshal.ThrowExceptionForHR(hr);
            try
            {
                var bgraValues = new int[file.Width * file.Height];
                for (var row = 0; row < file.Height; row++)
                    Marshal.Copy(
                        file.Scan0 + row * file.Stride,
                        bgraValues,
                        row * file.Width,
                        file.Width);
                return new TextureData((uint)file.Width, (uint)file.Height, bgraValues);
            } finally
            {
                NativeMethods.FreeDDS(ref file);
            }
        }
    }
}
