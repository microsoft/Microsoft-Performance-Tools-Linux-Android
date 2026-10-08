// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Buffers.Binary;
using System.IO;

namespace CtfUnitTest
{
    /// <summary>
    /// Metadata packet header layouts.
    /// </summary>
    public enum MetadataPacketLayout
    {
        /// <summary>CTF 1.8 section 7.1: 37 bytes, version 1.8.</summary>
        Ctf18,

        /// <summary>What LTTng 2.15+ writes for CTF 2 metadata: the 37-byte CTF 1.8 header with version 2.0.</summary>
        LttngCtf2,

        /// <summary>CTF2-PMETA-1.0: 44 bytes (3 reserved bytes and a header size field added), version 2.0.</summary>
        Ctf2Pmeta,
    }

    /// <summary>
    /// Builds packetized metadata streams.
    /// </summary>
    internal static class MetadataPackets
    {
        internal const uint Magic = 0x75d11d57;

        /// <param name="content">Metadata content</param>
        /// <param name="bigEndian">Byte order of the header's integer fields</param>
        /// <param name="layout">Packet header layout</param>
        /// <param name="maxContentBytesPerPacket">Content is split into packets of at most this many bytes</param>
        /// <param name="paddingBytes">Padding after the content of each packet</param>
        internal static byte[] Packetize(byte[] content, bool bigEndian, MetadataPacketLayout layout, int maxContentBytesPerPacket = int.MaxValue, int paddingBytes = 0)
        {
            int headerSize = layout == MetadataPacketLayout.Ctf2Pmeta ? 44 : 37;
            byte majorVersion = layout == MetadataPacketLayout.Ctf18 ? (byte)1 : (byte)2;
            byte minorVersion = layout == MetadataPacketLayout.Ctf18 ? (byte)8 : (byte)0;

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
                stream.Write(new byte[] { 0, 0, 0, majorVersion, minorVersion }, 0, 5);
                if (layout == MetadataPacketLayout.Ctf2Pmeta)
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
