using BCnEncoder.Decoder;
using BCnEncoder.Shared;
using BCnEncoder.Shared.ImageFiles;
using Microsoft.Toolkit.HighPerformance;
using System.Net;
using System.Runtime.InteropServices;

namespace Focus.Graphics.Formats
{
    public class ManagedDdsTextureSource : ITextureSource
    {
        private static readonly Task<Memory2D<ColorRgba32>> InvalidDecodeTask =
            Task.FromException<Memory2D<ColorRgba32>>(new Exception("Decoding not started."));

        public static ManagedDdsTextureSource FromFile(string path)
        {
            return new ManagedDdsTextureSource(async () => await File.ReadAllBytesAsync(path));
        }

        public static Task<ManagedDdsTextureSource> PreloadAsync(string path)
        {
            return PreloadAsync(async () => await File.ReadAllBytesAsync(path));
        }

        public static async Task<ManagedDdsTextureSource> PreloadAsync(Func<Task<Memory<byte>>> dataSource)
        {
            var source = new ManagedDdsTextureSource(dataSource);
            await source.PreloadAsync();
            return source;
        }

        private readonly Func<Task<Memory<byte>>> getFileDataAsync;

        private Task<Memory2D<ColorRgba32>> decodeTask = InvalidDecodeTask;

        public TexturePixelFormat Format => TexturePixelFormat.RGBA;

        internal ManagedDdsTextureSource(Func<Task<Memory<byte>>> getFileDataAsync)
        {
            this.getFileDataAsync = getFileDataAsync;
        }

        public TextureData GetTextureData()
        {
            var decoded = PreloadAsync().Result;
            var pixels = GetPixels(decoded);
            return new TextureData((uint)decoded.Width, (uint)decoded.Height, pixels);
        }

        public async Task<Memory2D<ColorRgba32>> PreloadAsync()
        {
            var tcs = new TaskCompletionSource<Memory2D<ColorRgba32>>();
            var previousDecodeTask =
                Interlocked.CompareExchange(ref decodeTask, tcs.Task, InvalidDecodeTask);
            if (previousDecodeTask != InvalidDecodeTask)
                return await previousDecodeTask;
            try
            {
                var fileData = await getFileDataAsync();
                using var fileStream = fileData.AsStream();
                var dds = DdsFile.Load(fileStream);
                var decoder = new BcDecoder();
                var format = decoder.GetFormat(dds);
                if (dds.Faces.Count == 6 && HasUniformFaces(dds)) // Cubemap
                {
                    var height = (int)dds.Faces[0].Height;
                    var width = (int)dds.Faces[0].Width;
                    Memory2D<ColorRgba32> colors = new ColorRgba32[height * 6, width];
                    for (int i = 0; i < 6; i++)
                    {
                        var face = await DecodeFace(decoder, dds.Faces[i], format);
                        face.CopyTo(colors.Slice(height * i, 0, height, width));
                    }
                    return colors;
                }
                var decoded = await decoder.Decode2DAsync(dds);
                tcs.SetResult(decoded);
                return decoded;
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
                throw;
            }
        }

        private static Task<Memory2D<ColorRgba32>> DecodeFace(
            BcDecoder decoder, DdsFace face, CompressionFormat format)
        {
            return decoder.DecodeRaw2DAsync(
                face.MipMaps[0].Data, (int)face.Width, (int)face.Height, format);
        }

        private static Span<int> GetPixels(Memory2D<ColorRgba32> decoded)
        {
            if (decoded.Span.TryGetSpan(out var decodedSpan))
                return MemoryMarshal.Cast<ColorRgba32, int>(decodedSpan);
            var pixels = new ColorRgba32[decoded.Width * decoded.Height];
            decoded.Span.CopyTo(pixels);
            return MemoryMarshal.Cast<ColorRgba32, int>(pixels);
        }

        private static bool HasUniformFaces(DdsFile dds)
        {
            return dds.Faces.Select(f => (f.Width, f.Height)).Distinct().Count() == 1;
        }
    }
}
