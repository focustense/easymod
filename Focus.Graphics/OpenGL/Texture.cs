using Silk.NET.OpenGL;
using System.Runtime.InteropServices;

namespace Focus.Graphics.OpenGL
{
    public class Texture : IDisposable
    {
        public static unsafe Texture FromArgb(GL gl, uint width, uint height, Span<int> argbPixels)
        {
            var bgraValues = ArgbToBgra(argbPixels);
            var texture = CreateAndBind(gl);
            fixed (void* data = &bgraValues[0])
            {
                gl.TexImage2D(
                    TextureTarget.Texture2D, 0, (int)InternalFormat.Rgba8, width, height, 0,
                    PixelFormat.Bgra, PixelType.UnsignedByte, data);
            }
            texture.SetDefaultParameters();
            return texture;
        }

        public static unsafe Texture FromMemory<T>(
            GL gl, Span<T> buffer, uint width, uint height,
            InternalFormat internalFormat = InternalFormat.Rgba8,
            PixelFormat pixelFormat = PixelFormat.Rgba)
            where T : unmanaged
        {
            var texture = CreateAndBind(gl);
            fixed (void* data = &buffer[0])
            {
                gl.TexImage2D(
                    TextureTarget.Texture2D, 0, internalFormat, width, height, 0, pixelFormat,
                    PixelType.UnsignedByte, data);
            }
            texture.SetDefaultParameters();
            return texture;
        }

        private static Texture CreateAndBind(GL gl)
        {
            var handle = gl.GenTexture();
            var texture = new Texture(gl, handle);
            texture.Bind();
            return texture;
        }

        private readonly GL gl;
        private readonly uint handle;

        private Texture(GL gl, uint handle)
        {
            this.gl = gl;
            this.handle = handle;
        }

        public void Bind(TextureUnit slot = TextureUnit.Texture0)
        {
            gl.ActiveTexture(slot);
            gl.BindTexture(TextureTarget.Texture2D, handle);
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

        private void SetDefaultParameters()
        {
            // Copied from the Silk example. Don't understand most of it yet.
            gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToEdge);
            gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);
            gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.LinearMipmapLinear);
            gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
            gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBaseLevel, 0);
            gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxLevel, 8);
            gl.GenerateMipmap(TextureTarget.Texture2D);
        }
    }
}
