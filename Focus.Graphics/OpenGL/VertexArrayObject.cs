﻿using Silk.NET.OpenGL;

namespace Focus.Graphics.OpenGL
{
    public class VertexArrayObject : IDisposable
    {
        public static VertexArrayObject Bind(GL gl, BufferObject vbo, BufferObject ebo)
        {
            var handle = gl.GenVertexArray();
            var vao = new VertexArrayObject(gl, handle, vbo.ElementSize);
            vao.Bind();
            vbo.Bind();
            ebo.Bind();
            return vao;
        }

        private readonly GL gl;
        private readonly uint handle;
        private readonly uint vertexSize;

        private VertexArrayObject(GL gl, uint handle, uint vertexSize)
        {
            this.gl = gl;
            this.handle = handle;
            this.vertexSize = vertexSize;
        }

        public void Bind()
        {
            gl.BindVertexArray(handle);
        }

        public void Dispose()
        {
            gl.DeleteVertexArray(handle);
            GC.SuppressFinalize(this);
        }

        public unsafe void EnableAttributeArray(
            uint index, int attributeCountPerVertex, VertexAttribPointerType attributeType,
            int vertexDataOffset)
        {
            EnableAttributeArray(
                index, attributeCountPerVertex, attributeType, (uint)vertexDataOffset);
        }

        public unsafe void EnableAttributeArray(
            uint index, int attributeCountPerVertex, VertexAttribPointerType attributeType,
            IntPtr vertexDataOffset)
        {
            EnableAttributeArray(
                index, attributeCountPerVertex, attributeType, vertexDataOffset.ToPointer());
        }

        public unsafe void EnableAttributeArray(
            uint index, int attributeCountPerVertex, VertexAttribPointerType attributeType,
            uint vertexDataOffset = 0)
        {
            var offsetPointer =
                vertexDataOffset > 0 ? (void*)vertexDataOffset : null;
            EnableAttributeArray(index, attributeCountPerVertex, attributeType, offsetPointer);
        }

        private unsafe void EnableAttributeArray(
            uint index, int attributeCountPerVertex, VertexAttribPointerType attributeType,
            void* offsetPointer = null)
        {
            gl.VertexAttribPointer(
                index, attributeCountPerVertex, attributeType, false, vertexSize, offsetPointer);
            gl.EnableVertexAttribArray(index);
        }
    }
}
