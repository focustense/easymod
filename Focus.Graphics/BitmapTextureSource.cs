using System.Drawing;
using System.Runtime.Versioning;

using PixelFormat = System.Drawing.Imaging.PixelFormat;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Focus.Graphics
{
    [SupportedOSPlatform("Windows")]
    public class BitmapTextureSource : ITextureSource
    {
        public static BitmapTextureSource FromFile(string path)
        {
            return new BitmapTextureSource(() => new Bitmap(path));
        }

        private readonly Func<Bitmap> loadBitmap;

        internal BitmapTextureSource(Func<Bitmap> bitmapSource)
        {
            loadBitmap = bitmapSource;
        }

        public TextureData GetTextureData()
        {
            using var bitmap = loadBitmap();
            var argbValues = new int[bitmap.Width * bitmap.Height];
            var bitmapData = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);
            try
            {
                Marshal.Copy(bitmapData.Scan0, argbValues, 0, argbValues.Length);
                return new TextureData((uint)bitmap.Width, (uint)bitmap.Height, argbValues);
            }
            finally
            {
                bitmap.UnlockBits(bitmapData);
            }
        }
    }
}
