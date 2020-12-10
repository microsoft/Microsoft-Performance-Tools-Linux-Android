// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.IO.Compression;
using CtfPlayback.Inputs;

namespace LttngCds.CtfExtensions.ZipArchiveInput
{
    internal sealed class LttngZipArchiveInputStream
        : ICtfInputStream
    {
        public LttngZipArchiveInputStream(ZipArchiveEntry archiveEntry)
        {
            this.StreamSource = archiveEntry.FullName;

            this.Stream = archiveEntry.Open();

            this.ByteCount = (ulong) archiveEntry.Length;
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