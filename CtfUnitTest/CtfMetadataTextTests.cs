// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Buffers.Binary;
using System.IO;
using System.IO.Compression;
using System.Text;
using CtfPlayback.Metadata;
using CtfPlayback.Metadata.AntlrParser;
using CtfPlayback.Metadata.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CtfUnitTest
{
    [TestClass]
    public class CtfMetadataTextTests
    {
        // Contains multi-byte UTF-8 characters, which small packets split across packet boundaries.
        private const string Text =
            "\u001e{\"type\":\"preamble\",\"version\":2}\n" +
            "\u001e{\"type\":\"trace-class\",\"environment\":{\"hostname\":\"h\u00f4te-\u00f1-\u65e5\u672c\"}}\n";

        [TestMethod]
        [DataRow(false, MetadataPacketLayout.Ctf18, 1)]
        [DataRow(true, MetadataPacketLayout.Ctf18, 1)]
        [DataRow(false, MetadataPacketLayout.LttngCtf2, 1)]
        [DataRow(true, MetadataPacketLayout.LttngCtf2, 1)]
        [DataRow(false, MetadataPacketLayout.LttngCtf2, 50)]
        [DataRow(true, MetadataPacketLayout.LttngCtf2, int.MaxValue)]
        [DataRow(false, MetadataPacketLayout.Ctf2Pmeta, 1)]
        [DataRow(true, MetadataPacketLayout.Ctf2Pmeta, 1)]
        [DataRow(false, MetadataPacketLayout.Ctf2Pmeta, 50)]
        [DataRow(true, MetadataPacketLayout.Ctf2Pmeta, int.MaxValue)]
        public void ReadsPacketizedMetadataInEitherByteOrder(bool bigEndian, MetadataPacketLayout layout, int maxContentBytesPerPacket)
        {
            byte[] packets = MetadataPackets.Packetize(Encoding.UTF8.GetBytes(Text), bigEndian, layout, maxContentBytesPerPacket, paddingBytes: 3);

            Assert.AreEqual(Text, CtfMetadataText.Read(packets));
        }

        [TestMethod]
        public void ReturnsTextMetadataUnchanged()
        {
            Assert.AreEqual(Text, CtfMetadataText.Read(Encoding.UTF8.GetBytes(Text)));

            // Shorter than a packet header.
            Assert.AreEqual("/* CTF 1.8 */", CtfMetadataText.Read(Encoding.UTF8.GetBytes("/* CTF 1.8 */")));
        }

        [TestMethod]
        public void RejectsCorruptPackets()
        {
            byte[] content = Encoding.UTF8.GetBytes(Text);

            var badMagic = MetadataPackets.Packetize(content, true, MetadataPacketLayout.Ctf2Pmeta, maxContentBytesPerPacket: 20);
            badMagic[44 + 20] ^= 0xFF;
            Assert.ThrowsException<InvalidDataException>(() => CtfMetadataText.Read(badMagic));

            var badHeaderSize = MetadataPackets.Packetize(content, true, MetadataPacketLayout.Ctf2Pmeta);
            BinaryPrimitives.WriteUInt32BigEndian(badHeaderSize.AsSpan(40), 37 * 8);
            Assert.ThrowsException<InvalidDataException>(() => CtfMetadataText.Read(badHeaderSize));

            var headerLargerThanPacket = MetadataPackets.Packetize(content, false, MetadataPacketLayout.Ctf2Pmeta);
            BinaryPrimitives.WriteUInt32LittleEndian(headerLargerThanPacket.AsSpan(40), 4096 * 8);
            Assert.ThrowsException<InvalidDataException>(() => CtfMetadataText.Read(headerLargerThanPacket));

            var badContentSize = MetadataPackets.Packetize(content, false, MetadataPacketLayout.Ctf18);
            BinaryPrimitives.WriteUInt32LittleEndian(badContentSize.AsSpan(24), (uint)((37 + content.Length) * 8) - 3);
            Assert.ThrowsException<InvalidDataException>(() => CtfMetadataText.Read(badContentSize));

            // A big-endian header read as little endian would give an absurd content size.
            var bigEndian = MetadataPackets.Packetize(content, true, MetadataPacketLayout.Ctf18);
            Assert.AreEqual(Text, CtfMetadataText.Read(bigEndian));
        }

        [TestMethod]
        public void ParsesBigEndianPacketizedCtf18Metadata()
        {
            // Real LTTng CTF 1.8 metadata, stored as little-endian packets.
            byte[] original;
            using (var trace = ZipFile.OpenRead(@"..\..\..\..\TestData\LTTng\lttng-kernel-trace.ctf"))
            using (var metadata = trace.GetEntry("kernel/metadata").Open())
            {
                original = CtfMetadataText.ReadAllBytes(metadata);
            }

            Assert.AreEqual(MetadataPackets.Magic, BinaryPrimitives.ReadUInt32LittleEndian(original));

            string text = CtfMetadataText.Read(original);
            var reference = new TestCtfMetadataCustomization();
            new CtfAntlrMetadataParser(reference, reference).Parse(new MemoryStream(original));

            var littleEndian = ParseWithVersionDetectingParser(original);
            var bigEndian = ParseWithVersionDetectingParser(MetadataPackets.Packetize(Encoding.UTF8.GetBytes(text), true, MetadataPacketLayout.Ctf18, 4096, 5));

            Assert.IsTrue(reference.Events.Count > 0);
            foreach (var parsed in new[] { littleEndian, bigEndian })
            {
                Assert.AreEqual(reference.Events.Count, parsed.Events.Count);
                Assert.AreEqual(reference.Streams.Count, parsed.Streams.Count);
                Assert.AreEqual(reference.Clocks.Count, parsed.Clocks.Count);
                Assert.AreEqual(reference.TraceDescriptor.Uuid, parsed.TraceDescriptor.Uuid);
            }
        }

        private static ICtfMetadata ParseWithVersionDetectingParser(byte[] metadata)
        {
            var builder = new TestCtfMetadataCustomization();
            return new CtfVersionDetectingMetadataParser(builder, builder, false).Parse(new MemoryStream(metadata));
        }
    }
}
