// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Buffers.Binary;
using System.IO;

namespace CtfUnitTest
{
    /// <summary>
    /// Builds packetized metadata streams (CTF 1.8 section 7.1, and CTF2-PMETA-1.0).
    /// </summary>
    internal static class MetadataPackets
    {
        internal const uint Magic = 0x75d11d57;

        /// <param name="content">Metadata content</param>
        /// <param name="bigEndian">Byte order of the header's integer fields</param>
        /// <param name="majorVersion">1 for CTF 1.8 (37-byte header), 2 for CTF 2 (44-byte header)</param>
        /// <param name="maxContentBytesPerPacket">Content is split into packets of at most this many bytes</param>
        /// <param name="paddingBytes">Padding after the content of each packet</param>
        internal static byte[] Packetize(byte[] content, bool bigEndian, byte majorVersion, int maxContentBytesPerPacket = int.MaxValue, int paddingBytes = 0)
        {
            int headerSize = majorVersion >= 2 ? 44 : 37;

            var stream = new MemoryStream();
            int offset = 0;
            do
            {
                int chunk = Math.Min(maxContentBytesPerPacket, content.Length - offset);
                uint contentBits = (uint)((headerSize + chunk) * 8);
                uint packetBits = (uint)((headerSize + chunk + paddingBytes) * 8);

                WriteUInt32(stream, Magic, bigEndian);
                stream.Write(new byte[16], 0, 16);
                WriteUInt32(stream, 0, bigEndian);
                WriteUInt32(stream, contentBits, bigEndian);
                WriteUInt32(stream, packetBits, bigEndian);
                stream.Write(new byte[] { 0, 0, 0, majorVersion, majorVersion >= 2 ? (byte)0 : (byte)8 }, 0, 5);
                if (majorVersion >= 2)
                {
                    stream.Write(new byte[3], 0, 3);
                    WriteUInt32(stream, (uint)(headerSize * 8), bigEndian);
                }

                stream.Write(content, offset, chunk);
                stream.Write(new byte[paddingBytes], 0, paddingBytes);
                offset += chunk;
            }
            while (offset < content.Length);

            return stream.ToArray();
        }

        internal static void WriteUInt32(Stream stream, uint value, bool bigEndian)
        {
            var bytes = new byte[4];
            if (bigEndian)
            {
                BinaryPrimitives.WriteUInt32BigEndian(bytes, value);
            }
            else
            {
                BinaryPrimitives.WriteUInt32LittleEndian(bytes, value);
            }

            stream.Write(bytes, 0, bytes.Length);
        }
    }
}
