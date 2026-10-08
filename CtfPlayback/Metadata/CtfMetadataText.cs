// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;

namespace CtfPlayback.Metadata
{
    /// <summary>
    /// Helpers for reading a metadata stream, which may be plain text or packetized
    /// (see https://diamon.org/ctf/#spec7.1 and https://diamon.org/ctf/files/CTF2-PMETA-1.0.html).
    /// </summary>
    internal static class CtfMetadataText
    {
        private const uint PacketMagic = 0x75d11d57;

        // CTF 1.8: magic(4) + uuid(16) + checksum(4) + content_size(4) + packet_size(4) + 5 single-byte fields
        private const int Ctf1PacketHeaderSize = 37;

        // CTF2-PMETA-1.0: the CTF 1.8 fields, 3 reserved bytes, then the header size in bits (352 = 44 bytes).
        private const int Ctf2MinimumPacketHeaderSize = 44;

        private const int ContentSizeOffset = 24;
        private const int PacketSizeOffset = 28;
        private const int MajorVersionOffset = 35;
        private const int Ctf2HeaderSizeOffset = 40;

        internal static byte[] ReadAllBytes(Stream stream)
        {
            using (var memoryStream = new MemoryStream())
            {
                stream.CopyTo(memoryStream);
                return memoryStream.ToArray();
            }
        }

        /// <summary>
        /// Returns the metadata text, removing packet headers if the metadata is packetized.
        /// </summary>
        internal static string Read(byte[] metadata)
        {
            if (metadata.Length < Ctf1PacketHeaderSize || !TryGetPacketByteOrder(metadata, 0, out _))
            {
                return Encoding.UTF8.GetString(metadata);
            }

            // Content is decoded once at the end because a multi-byte UTF-8 character may span two packets.
            var content = new MemoryStream(metadata.Length);
            long offset = 0;
            while (offset + Ctf1PacketHeaderSize <= metadata.Length)
            {
                int packetOffset = (int)offset;

                // The encoding of the magic number gives the byte order of the header's other integer fields.
                if (!TryGetPacketByteOrder(metadata, packetOffset, out bool bigEndian))
                {
                    throw new InvalidDataException("Metadata stream seems to be corrupt: invalid packet magic number.");
                }

                uint contentBits = ReadUInt32(metadata, packetOffset + ContentSizeOffset, bigEndian);
                uint packetBits = ReadUInt32(metadata, packetOffset + PacketSizeOffset, bigEndian);
                if ((contentBits % 8) != 0 || (packetBits % 8) != 0)
                {
                    throw new InvalidDataException("Metadata stream seems to be corrupt: packet size is not a whole number of bytes.");
                }

                int headerBytes = GetHeaderSize(metadata, packetOffset, bigEndian);
                long contentBytes = contentBits / 8;
                long packetBytes = packetBits / 8;
                if (contentBytes < headerBytes || packetBytes < contentBytes || offset + contentBytes > metadata.Length)
                {
                    throw new InvalidDataException("Metadata stream seems to be corrupt: invalid packet size.");
                }

                content.Write(metadata, packetOffset + headerBytes, (int)contentBytes - headerBytes);
                offset += packetBytes;
            }

            return Encoding.UTF8.GetString(content.GetBuffer(), 0, (int)content.Length);
        }

        private static int GetHeaderSize(byte[] metadata, int packetOffset, bool bigEndian)
        {
            // CTF 1.8 metadata packets are version 1.8; CTF 2 packets are version 2.0 and give their header size.
            if (metadata[packetOffset + MajorVersionOffset] < 2)
            {
                return Ctf1PacketHeaderSize;
            }

            if (packetOffset + Ctf2MinimumPacketHeaderSize > metadata.Length)
            {
                throw new InvalidDataException("Metadata stream seems to be corrupt: truncated packet header.");
            }

            uint headerBits = ReadUInt32(metadata, packetOffset + Ctf2HeaderSizeOffset, bigEndian);
            if ((headerBits % 8) != 0 || headerBits < Ctf2MinimumPacketHeaderSize * 8)
            {
                throw new InvalidDataException("Metadata stream seems to be corrupt: invalid packet header size.");
            }

            return (int)(headerBits / 8);
        }

        private static bool TryGetPacketByteOrder(byte[] metadata, int packetOffset, out bool bigEndian)
        {
            var magic = new ReadOnlySpan<byte>(metadata, packetOffset, sizeof(uint));
            bigEndian = BinaryPrimitives.ReadUInt32BigEndian(magic) == PacketMagic;
            return bigEndian || BinaryPrimitives.ReadUInt32LittleEndian(magic) == PacketMagic;
        }

        private static uint ReadUInt32(byte[] metadata, int index, bool bigEndian)
        {
            var bytes = new ReadOnlySpan<byte>(metadata, index, sizeof(uint));
            return bigEndian ? BinaryPrimitives.ReadUInt32BigEndian(bytes) : BinaryPrimitives.ReadUInt32LittleEndian(bytes);
        }
    }
}
