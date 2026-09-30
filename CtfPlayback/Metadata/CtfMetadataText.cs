// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Text;

namespace CtfPlayback.Metadata
{
    /// <summary>
    /// Helpers for reading a metadata stream, which may be plain text or packetized (see https://diamon.org/ctf/#spec7.1).
    /// </summary>
    internal static class CtfMetadataText
    {
        private const uint PacketMagic = 0x75d11d57;

        // magic(4) + uuid(16) + checksum(4) + content_size(4) + packet_size(4) + 5 single-byte fields
        private const int PacketHeaderSize = 37;

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
            if (metadata.Length < PacketHeaderSize || BitConverter.ToUInt32(metadata, 0) != PacketMagic)
            {
                return Encoding.UTF8.GetString(metadata);
            }

            var sb = new StringBuilder();
            int offset = 0;
            while (offset + PacketHeaderSize <= metadata.Length)
            {
                if (BitConverter.ToUInt32(metadata, offset) != PacketMagic)
                {
                    throw new InvalidDataException("Metadata stream seems to be corrupt: invalid packet magic number.");
                }

                int contentBytes = (int)(BitConverter.ToUInt32(metadata, offset + 24) / 8);
                int packetBytes = (int)(BitConverter.ToUInt32(metadata, offset + 28) / 8);
                if (contentBytes < PacketHeaderSize || packetBytes < contentBytes || offset + contentBytes > metadata.Length)
                {
                    throw new InvalidDataException("Metadata stream seems to be corrupt: invalid packet size.");
                }

                sb.Append(Encoding.UTF8.GetString(metadata, offset + PacketHeaderSize, contentBytes - PacketHeaderSize));
                offset += packetBytes;
            }

            return sb.ToString();
        }
    }
}
