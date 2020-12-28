// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using CtfPlayback.Inputs;

namespace LTTngCds.CtfExtensions.FolderInput
{
    internal sealed class LTTngFileInputStream
        : ICtfInputStream
    {
        public LTTngFileInputStream(string fileName)
        {
            if (!File.Exists(fileName))
            {
                throw new ArgumentException($"File does not exist: {fileName}", nameof(fileName));
            }

            this.StreamSource = fileName;

            this.Stream = File.OpenRead(fileName);

            this.ByteCount = (ulong)Stream.Length;
        }

        public string StreamSource { get; }

        public Stream Stream { get; }

        public ulong ByteCount { get; }

        public void Dispose()
        {
            this.Stream?.Dispose();
        }
    }
}