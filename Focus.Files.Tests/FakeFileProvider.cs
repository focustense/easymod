﻿using System;
using System.Collections.Generic;

namespace Focus.Files.Tests
{
    // Mocks don't work well with IFileProvider due to the use of ReadOnlySpan.
    class FakeFileProvider : IFileProvider
    {
        private readonly Dictionary<string, byte[]> files = new();

        public bool Exists(string fileName)
        {
            return files.ContainsKey(fileName);
        }

        public void PutFile(string fileName, byte[] data)
        {
            files[fileName] = data;
        }

        public ReadOnlySpan<byte> ReadBytes(string fileName)
        {
            return files.TryGetValue(fileName, out var data) ? data : null;
        }
    }
}