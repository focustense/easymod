using Silk.NET.OpenGL;
using System.Runtime.InteropServices;

namespace Focus.Graphics.OpenGL
{
    public class Texture : IDisposable
    {
        public static unsafe Texture FromArgb(
            GL gl, TextureUnit slot, uint width, uint height, Span<int> argbPixels,
            IEnumerable<CubemapFace>? cubemapFaceOrder = null)
        {
            var bgraValues = ArgbToBgra(argbPixels);
            return FromMemory(
                gl, slot, bgraValues, width, height, InternalFormat.Rgba8, PixelFormat.Bgra,
                cubemapFaceOrder);
        }

        public static unsafe Texture FromBgra(
            GL gl, TextureUnit slot, uint width, uint height, Span<int> bgraPixels,
            IEnumerable<CubemapFace>? cubemapFaceOrder = null)
        {
            return FromMemory(
                gl, slot, bgraPixels, width, height, InternalFormat.Rgba8, PixelFormat.Bgra,
                cubemapFaceOrder);
        }

        public static unsafe Texture FromRgba(
            GL gl, TextureUnit slot, uint width, uint height, Span<int> rgbaPixels,
            IEnumerable<CubemapFace>? cubemapFaceOrder = null)
        {
            return FromMemory(
                gl, slot, rgbaPixels, width, height, InternalFormat.Rgba8, PixelFormat.Rgba,
                cubemapFaceOrder);
        }

        public static unsafe Texture FromMemory<T>(
            GL gl, TextureUnit slot, Span<T> buffer, uint width, uint height,
            InternalFormat internalFormat = InternalFormat.Rgba8,
            PixelFormat pixelFormat = PixelFormat.Rgba,
            IEnumerable<CubemapFace>? cubemapFaceOrder = null)
            where T : unmanaged
        {
            Texture texture;
            fixed (void* data = &buffer[0])
            {
                if (cubemapFaceOrder != null)
                {
                    var faceOrder = cubemapFaceOrder.Distinct().ToList();
                    if (faceOrder.Count != 6)
                        throw new ArgumentException(
                            "Invalid cubemap face order; must specify exactly 6 distinct faces.",
                            nameof(cubemapFaceOrder));
                    var faceHeight = height / 6;
                    texture = CreateAndBind(gl, slot, TextureTarget.TextureCubeMap);
                    for (int i = 0; i < faceOrder.Count; i++)
                    {
                        var face = faceOrder[i];
                        var faceTarget = GetCubemapFaceTarget(face);
                        gl.TexImage2D(
                            faceTarget, 0, internalFormat, width, faceHeight, 0, pixelFormat,
                            PixelType.UnsignedByte,
                            (byte*)data + i * faceHeight * width * sizeof(int));
                    }
                    texture.SetDefaultParameters(TextureTarget.TextureCubeMap, false);
                }
                else
                {
                    texture = CreateAndBind(gl, slot, TextureTarget.Texture2D);
                    gl.TexImage2D(
                        TextureTarget.Texture2D, 0, internalFormat, width, height, 0, pixelFormat,
                        PixelType.UnsignedByte, data);
                    texture.SetDefaultParameters(TextureTarget.Texture2D);
                }
            }
            return texture;
        }

        private static Texture CreateAndBind(GL gl, TextureUnit slot, TextureTarget target)
        {
            var handle = gl.GenTexture();
            var texture = new Texture(gl, slot, handle, target);
            texture.Bind();
            return texture;
        }

        public int SlotIndex => slot - TextureUnit.Texture0;

        private readonly GL gl;
        private readonly TextureUnit slot;
        private readonly uint handle;
        private readonly TextureTarget target;

        private Texture(GL gl, TextureUnit slot, uint handle, TextureTarget target)
        {
            this.gl = gl;
            this.slot = slot;
            this.handle = handle;
            this.target = target;
        }

        public void Bind()
        {
            gl.ActiveTexture(slot);
            gl.BindTexture(target, handle);
        }

        public void Dispose()
        {
            gl.DeleteTexture(handle);
            GC.SuppressFinalize(this);
        }

        private static Span<byte> ArgbToBgra(Span<int> argbValues)
        {
            // If we're little-endian (and we probably are, on a normal PC), then this is easy.
            // Marshaling the integer values as bytes will already produce a sequence that has the
            // last byte of each word first.
            if (BitConverter.IsLittleEndian)
                return MemoryMarshal.AsBytes(argbValues);

            // Otherwise, we can fix this with a double reversal: first reverse the words, then
            // marshal as bytes and reverse the bytes (which returns us to the original word order
            // but with the order of bytes reversed for each word).
            var reverseArgbValues = argbValues;
            reverseArgbValues.Reverse();
            Span<byte> bgraValues = MemoryMarshal.AsBytes(reverseArgbValues);
            bgraValues.Reverse();
            return bgraValues;
        }

        private static TextureTarget GetCubemapFaceTarget(CubemapFace face) => face switch
        {
            CubemapFace.Right => TextureTarget.TextureCubeMapPositiveX,
            CubemapFace.Left => TextureTarget.TextureCubeMapNegativeX,
            CubemapFace.Top => TextureTarget.TextureCubeMapPositiveY,
            CubemapFace.Bottom => TextureTarget.TextureCubeMapNegativeY,
            CubemapFace.Front => TextureTarget.TextureCubeMapPositiveZ,
            CubemapFace.Back => TextureTarget.TextureCubeMapNegativeZ,
            _ => throw new ArgumentException($"Unknown cubemap face {face}", nameof(face))
        };

        private void SetDefaultParameters(TextureTarget target, bool enableMipMap = true)
        {
            // Copied from the Silk example. Don't understand most of it yet.
            gl.TexParameter(target, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToEdge);
            gl.TexParameter(target, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);
            gl.TexParameter(target, TextureParameterName.TextureMinFilter,
                enableMipMap ? (int)GLEnum.LinearMipmapLinear : (int)GLEnum.Linear);
            gl.TexParameter(target, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
            gl.TexParameter(target, TextureParameterName.TextureBaseLevel, 0);
            gl.TexParameter(target, TextureParameterName.TextureMaxLevel, 8);
            if (enableMipMap)
                gl.GenerateMipmap(target);
        }
    }
}
