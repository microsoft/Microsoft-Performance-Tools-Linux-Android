// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CtfPlayback;
using CtfPlayback.FieldValues;
using CtfPlayback.Metadata;
using CtfPlayback.Metadata.Ctf2;
using CtfPlayback.Metadata.Interfaces;
using CtfPlayback.Metadata.InternalHelpers;
using CtfPlayback.Metadata.TypeInterfaces;
using CtfPlayback.Metadata.Types;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CtfUnitTest
{
    [TestClass]
    public class Ctf2MetadataTests
    {
        // Modeled on the metadata written by LTTng 2.16 for a kernel trace with perf counter contexts.
        private static readonly string[] LTTngKernelFragments =
        {
            @"{""type"":""preamble"",""version"":2,""uuid"":[55,65,178,152,114,22,242,74,147,79,201,46,48,147,240,64]}",
            @"{""type"":""field-class-alias"",""name"":""u32-erc-id"",""field-class"":{""type"":""fixed-length-unsigned-integer"",""length"":32,""alignment"":8,""byte-order"":""little-endian"",""roles"":[""event-record-class-id""]}}",
            @"{""type"":""field-class-alias"",""name"":""u64-ts"",""field-class"":{""type"":""fixed-length-unsigned-integer"",""length"":64,""alignment"":8,""byte-order"":""little-endian"",""roles"":[""default-clock-timestamp""]}}",
            @"{""type"":""field-class-alias"",""name"":""er-header-compact"",""field-class"":{""type"":""structure"",""minimum-alignment"":8,""member-classes"":[{""name"":""id"",""field-class"":{""type"":""fixed-length-unsigned-integer"",""length"":5,""byte-order"":""little-endian"",""mappings"":{""compact"":[[0,30]],""extended"":[[31,31]]},""roles"":[""event-record-class-id""]}},{""name"":""v"",""field-class"":{""type"":""variant"",""selector-field-location"":{""path"":[""id""]},""options"":[{""name"":""compact"",""selector-field-ranges"":[[0,30]],""field-class"":{""type"":""structure"",""member-classes"":[{""name"":""timestamp"",""field-class"":{""type"":""fixed-length-unsigned-integer"",""length"":27,""byte-order"":""little-endian"",""roles"":[""default-clock-timestamp""]}}]}},{""name"":""extended"",""selector-field-ranges"":[[31,31]],""field-class"":{""type"":""structure"",""member-classes"":[{""name"":""id"",""field-class"":""u32-erc-id""},{""name"":""timestamp"",""field-class"":""u64-ts""}]}}]}}]}}",
            @"{""type"":""field-class-alias"",""name"":""pkt-ctx"",""field-class"":{""type"":""structure"",""member-classes"":[{""name"":""timestamp_begin"",""field-class"":""u64-ts""},{""name"":""timestamp_end"",""field-class"":{""type"":""fixed-length-unsigned-integer"",""length"":64,""alignment"":8,""byte-order"":""little-endian"",""roles"":[""packet-end-default-clock-timestamp""]}},{""name"":""content_size"",""field-class"":{""type"":""fixed-length-unsigned-integer"",""length"":64,""alignment"":8,""byte-order"":""little-endian""}},{""name"":""packet_size"",""field-class"":{""type"":""fixed-length-unsigned-integer"",""length"":64,""alignment"":8,""byte-order"":""little-endian""}},{""name"":""events_discarded"",""field-class"":{""type"":""fixed-length-unsigned-integer"",""length"":64,""alignment"":8,""byte-order"":""little-endian""}},{""name"":""cpu_id"",""field-class"":{""type"":""fixed-length-unsigned-integer"",""length"":32,""alignment"":8,""byte-order"":""little-endian""}}]}}",
            @"{""type"":""field-class-alias"",""name"":""pkt-header"",""field-class"":{""type"":""structure"",""member-classes"":[{""name"":""magic"",""field-class"":{""type"":""fixed-length-unsigned-integer"",""length"":32,""alignment"":8,""byte-order"":""little-endian""}},{""name"":""uuid"",""field-class"":{""type"":""static-length-blob"",""length"":16}},{""name"":""stream_id"",""field-class"":{""type"":""fixed-length-unsigned-integer"",""length"":32,""alignment"":8,""byte-order"":""little-endian""}}]}}",
            @"{""type"":""trace-class"",""uid"":""3741b298-7216-f24a-934f-c92e3093f040"",""environment"":{""domain"":""kernel"",""tracer_major"":2,""hostname"":""host""},""packet-header-field-class"":""pkt-header""}",
            @"{""type"":""clock-class"",""id"":""monotonic"",""name"":""monotonic"",""frequency"":1000000000,""origin"":""unix-epoch"",""offset-from-origin"":{""seconds"":10,""cycles"":5}}",
            @"{""type"":""data-stream-class"",""id"":1,""default-clock-class-id"":""monotonic"",""packet-context-field-class"":""pkt-ctx"",""event-record-header-field-class"":""er-header-compact"",""event-record-common-context-field-class"":{""type"":""structure"",""member-classes"":[{""name"":""tid"",""field-class"":{""type"":""fixed-length-signed-integer"",""length"":32,""byte-order"":""little-endian"",""alignment"":8}}]}}",
            @"{""type"":""data-stream-class"",""id"":0,""default-clock-class-id"":""monotonic"",""packet-context-field-class"":""pkt-ctx"",""event-record-header-field-class"":""er-header-compact"",""event-record-common-context-field-class"":{""type"":""structure"",""member-classes"":[{""name"":""perf_cpu_cycles"",""field-class"":{""type"":""fixed-length-unsigned-integer"",""length"":64,""byte-order"":""little-endian"",""alignment"":8}},{""name"":""_callstack_kernel_length"",""field-class"":{""type"":""fixed-length-unsigned-integer"",""length"":32,""byte-order"":""little-endian"",""alignment"":8}},{""name"":""callstack_kernel"",""field-class"":{""type"":""dynamic-length-array"",""element-field-class"":{""type"":""fixed-length-unsigned-integer"",""length"":64,""byte-order"":""little-endian"",""alignment"":8,""preferred-display-base"":16},""length-field-location"":{""path"":[""_callstack_kernel_length""]}}}]}}",
            @"{""type"":""event-record-class"",""data-stream-class-id"":1,""id"":0,""name"":""sched_wakeup"",""payload-field-class"":{""type"":""structure"",""member-classes"":[{""name"":""comm"",""field-class"":{""type"":""static-length-string"",""length"":16}},{""name"":""tid"",""field-class"":{""type"":""fixed-length-signed-integer"",""length"":32,""byte-order"":""little-endian"",""alignment"":8}}]}}",
            @"{""type"":""event-record-class"",""data-stream-class-id"":0,""id"":0,""name"":""sched_switch"",""attributes"":{""lttng.org,2009"":{""log-level"":13}},""payload-field-class"":{""type"":""structure"",""member-classes"":[{""name"":""prev_tid"",""field-class"":{""type"":""fixed-length-signed-integer"",""length"":32,""byte-order"":""little-endian"",""alignment"":8}},{""name"":""filename"",""field-class"":{""type"":""null-terminated-string""}},{""name"":""address_ipv4"",""field-class"":{""type"":""fixed-length-unsigned-integer"",""length"":32,""byte-order"":""big-endian"",""alignment"":8,""preferred-display-base"":16}},{""name"":""ihl"",""field-class"":{""type"":""fixed-length-unsigned-integer"",""length"":4,""byte-order"":""big-endian""}},{""name"":""ratio"",""field-class"":{""type"":""fixed-length-floating-point-number"",""length"":64,""byte-order"":""little-endian"",""alignment"":8}},{""name"":""ratio_be"",""field-class"":{""type"":""fixed-length-floating-point-number"",""length"":32,""byte-order"":""big-endian"",""alignment"":8}}]}}",
        };

        private static string LTTngKernelMetadata => string.Concat(LTTngKernelFragments.Select(fragment => "\u001e" + fragment + "\n"));

        [TestMethod]
        public void DetectsCtf2Metadata()
        {
            Assert.IsTrue(Ctf2MetadataParser.IsCtf2Metadata(LTTngKernelMetadata));
            Assert.IsTrue(Ctf2MetadataParser.IsCtf2Metadata("\n \u001e{}"));
            Assert.IsFalse(Ctf2MetadataParser.IsCtf2Metadata("/* CTF 1.8 */ trace { major = 1; };"));
            Assert.IsFalse(Ctf2MetadataParser.IsCtf2Metadata(string.Empty));
        }

        [TestMethod]
        public void ParsesTraceClockAndStreams()
        {
            var builder = Parse(LTTngKernelMetadata);

            Assert.AreEqual(Guid.Parse("3741b298-7216-f24a-934f-c92e3093f040"), builder.TraceDescriptor.Uuid);
            Assert.AreEqual("kernel", builder.EnvironmentDescriptor.Properties["domain"]);
            Assert.AreEqual("2", builder.EnvironmentDescriptor.Properties["tracer_major"]);
            CollectionAssert.AreEqual(
                new[] { "magic", "uuid", "stream_id" },
                builder.TraceDescriptor.PacketHeader.Fields.Select(f => f.Name).ToArray());

            var clock = builder.ClocksByName["monotonic"];
            Assert.AreEqual(1000000000ul, clock.Frequency);
            Assert.AreEqual(10000000005ul, clock.Offset);

            // Streams are declared out of order in the metadata but are stored by id.
            CollectionAssert.AreEqual(new uint[] { 0, 1 }, builder.Streams.Select(s => s.Id).ToArray());
        }

        [TestMethod]
        public void EventHeaderVariantSelectorIsAnEnumeration()
        {
            var builder = Parse(LTTngKernelMetadata);
            var header = builder.Streams[0].EventHeader;

            var id = header.GetField("id").TypeDescriptor as ICtfEnumDescriptor;
            Assert.IsNotNull(id);
            Assert.AreEqual("compact", id.GetName(5ul));
            Assert.AreEqual("extended", id.GetName(31ul));

            var variant = header.GetField("v").TypeDescriptor as ICtfVariantDescriptor;
            Assert.IsNotNull(variant);
            Assert.AreEqual("id", variant.Switch);

            var compactTimestamp = ((ICtfStructDescriptor)variant.GetVariant("compact").TypeDescriptor).GetField("timestamp").TypeDescriptor as ICtfIntegerDescriptor;
            Assert.AreEqual(27, compactTimestamp.Size);
            Assert.AreEqual("clock.monotonic.value", compactTimestamp.Map);
        }

        [TestMethod]
        public void EventFieldNamesArePrefixedLikeCtf18()
        {
            var builder = Parse(LTTngKernelMetadata);

            var context = builder.Streams[0].EventContext;
            CollectionAssert.AreEqual(
                new[] { "_perf_cpu_cycles", "__callstack_kernel_length", "_callstack_kernel" },
                context.Fields.Select(f => f.Name).ToArray());

            var callstack = context.GetField("_callstack_kernel").TypeDescriptor as ICtfArrayDescriptor;
            Assert.AreEqual("__callstack_kernel_length", callstack.Index);

            // Header and packet scopes keep their names.
            Assert.IsNotNull(builder.Streams[0].PacketContext.GetField("timestamp_begin"));
        }

        [TestMethod]
        public void EventsAreIdentifiedByStreamAndId()
        {
            var builder = Parse(LTTngKernelMetadata);

            Assert.AreEqual(2, builder.AddedEvents.Count);

            var sched_switch = builder.AddedEvents.Single(e => e.Assignments["name"] == "sched_switch");
            Assert.AreEqual("0", sched_switch.Assignments["stream_id"]);
            Assert.AreEqual("0", sched_switch.Assignments["id"]);
            Assert.AreEqual("13", sched_switch.Assignments["loglevel"]);

            var sched_wakeup = builder.AddedEvents.Single(e => e.Assignments["name"] == "sched_wakeup");
            Assert.AreEqual("1", sched_wakeup.Assignments["stream_id"]);
            Assert.AreEqual("0", sched_wakeup.Assignments["id"]);

            var payload = (ICtfStructDescriptor)sched_wakeup.TypeDeclarations["fields"];
            var comm = payload.GetField("_comm").TypeDescriptor as ICtfArrayDescriptor;
            Assert.IsNotNull(comm);
            Assert.AreEqual("16", comm.Index);
            Assert.AreEqual("UTF8", ((ICtfIntegerDescriptor)comm.Type).Encoding);

            var switchPayload = (ICtfStructDescriptor)sched_switch.TypeDeclarations["fields"];
            Assert.IsInstanceOfType(switchPayload.GetField("_filename").TypeDescriptor, typeof(ICtfStringDescriptor));
            Assert.AreEqual(4, ((ICtfIntegerDescriptor)switchPayload.GetField("_ihl").TypeDescriptor).Size);

            var ratio = switchPayload.GetField("_ratio").TypeDescriptor as ICtfFloatingPointDescriptor;
            Assert.IsNotNull(ratio);
            Assert.AreEqual(11, ratio.Exponent);
            Assert.AreEqual(53, ratio.Mantissa);
        }

        [TestMethod]
        public void VersionDetectingParserHandlesPacketizedCtf2Metadata()
        {
            byte[] text = Encoding.UTF8.GetBytes(LTTngKernelMetadata);
            var stream = new MemoryStream();
            var writer = new BinaryWriter(stream);
            writer.Write(0x75d11d57u);
            writer.Write(new byte[16]);
            writer.Write(0u);
            writer.Write((uint)((37 + text.Length) * 8));
            writer.Write((uint)((37 + text.Length) * 8));
            writer.Write(new byte[] { 0, 0, 0, 2, 0 });
            writer.Write(text);
            stream.Position = 0;

            var builder = new Ctf2TestMetadataBuilder();
            new CtfVersionDetectingMetadataParser(null, builder, true).Parse(stream);

            Assert.AreEqual(2, builder.Streams.Count);
            Assert.AreEqual(2, builder.AddedEvents.Count);
        }

        [TestMethod]
        public void RejectsUnsupportedVersion()
        {
            Assert.ThrowsException<CtfMetadataException>(() => Parse("\u001e{\"type\":\"preamble\",\"version\":3}"));
        }

        [TestMethod]
        public void ReadsBigEndianIntegers()
        {
            var builder = Parse(LTTngKernelMetadata);
            var sched_switch = builder.AddedEvents.Single(e => e.Assignments["name"] == "sched_switch");
            var address = (CtfIntegerDescriptor)((ICtfStructDescriptor)sched_switch.TypeDeclarations["fields"]).GetField("_address_ipv4").TypeDescriptor;

            var value = (CtfIntegerValue)address.Read(new byte[] { 127, 0, 0, 1 }, 4);
            Assert.AreEqual(0x7F000001ul, value.Value.ValueAsUlong);
        }

        [TestMethod]
        public void ReadsBigEndianFloatingPoint()
        {
            var builder = Parse(LTTngKernelMetadata);
            var sched_switch = builder.AddedEvents.Single(e => e.Assignments["name"] == "sched_switch");
            var payload = (ICtfStructDescriptor)sched_switch.TypeDeclarations["fields"];

            // 123.4f = 0x42F6CCCD
            var bigEndian = (CtfFloatingPointDescriptor)payload.GetField("_ratio_be").TypeDescriptor;
            Assert.AreEqual(123.4f, ((CtfFloatValue)bigEndian.Read(new byte[] { 0x42, 0xF6, 0xCC, 0xCD }, 4)).Value);

            // -2.0 = 0xC000000000000000
            var littleEndian = (CtfFloatingPointDescriptor)payload.GetField("_ratio").TypeDescriptor;
            Assert.AreEqual(-2.0, ((CtfDoubleValue)littleEndian.Read(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0xC0 }, 8)).Value);
        }

        [TestMethod]
        public void ReadsVariableLengthIntegers()
        {
            var unsigned = new Ctf2VariableLengthIntegerDescriptor(false, 10, null);
            var signed = new Ctf2VariableLengthIntegerDescriptor(true, 10, null);

            Assert.AreEqual(624485ul, ((CtfIntegerValue)unsigned.Read(new BytePacketReader(0xE5, 0x8E, 0x26))).Value.ValueAsUlong);
            Assert.AreEqual(-123456L, ((CtfIntegerValue)signed.Read(new BytePacketReader(0xC0, 0xBB, 0x78))).Value.ValueAsLong);
        }

        [TestMethod]
        public void JsonReaderHandlesLargeNumbersAndEscapes()
        {
            var json = (Dictionary<string, object>)Ctf2Json.Parse(@"{ ""a"": 18446744073709551615, ""b"": ""x\""\u0041\n"", ""c"": [true, false, null, -5] }");

            Assert.AreEqual(ulong.MaxValue, ((Ctf2JsonNumber)json["a"]).AsULong());
            Assert.AreEqual("x\"A\n", json["b"]);
            var array = (List<object>)json["c"];
            Assert.AreEqual(true, array[0]);
            Assert.AreEqual(false, array[1]);
            Assert.IsNull(array[2]);
            Assert.AreEqual(-5L, ((Ctf2JsonNumber)array[3]).AsLong());

            Assert.ThrowsException<CtfMetadataException>(() => Ctf2Json.Parse("{\"a\": }"));
        }

        private static Ctf2TestMetadataBuilder Parse(string metadata)
        {
            var builder = new Ctf2TestMetadataBuilder();
            new Ctf2MetadataParser(builder, prefixEventFieldNamesWithUnderscore: true).Parse(metadata);
            return builder;
        }

        private sealed class AddedEvent
        {
            public IReadOnlyDictionary<string, string> Assignments { get; set; }

            public IReadOnlyDictionary<string, ICtfTypeDescriptor> TypeDeclarations { get; set; }
        }

        private sealed class Ctf2TestMetadataBuilder
            : ICtfMetadataBuilder
        {
            private readonly List<ICtfClockDescriptor> clocks = new List<ICtfClockDescriptor>();
            private readonly List<ICtfStreamDescriptor> streams = new List<ICtfStreamDescriptor>();

            public List<AddedEvent> AddedEvents { get; } = new List<AddedEvent>();

            public ICtfTraceDescriptor TraceDescriptor { get; private set; }

            public ICtfEnvironmentDescriptor EnvironmentDescriptor { get; private set; }

            public IReadOnlyList<ICtfClockDescriptor> Clocks => this.clocks;

            public IReadOnlyDictionary<string, ICtfClockDescriptor> ClocksByName => this.clocks.ToDictionary(c => c.Name);

            public IReadOnlyList<ICtfStreamDescriptor> Streams => this.streams;

            public IReadOnlyList<ICtfEventDescriptor> Events => Array.Empty<ICtfEventDescriptor>();

            public void SetTraceDescriptor(ICtfTraceDescriptor traceDescriptor) => this.TraceDescriptor = traceDescriptor;

            public void SetEnvironmentDescriptor(ICtfEnvironmentDescriptor environmentDescriptor) => this.EnvironmentDescriptor = environmentDescriptor;

            public void AddEvent(IReadOnlyDictionary<string, string> assignments, IReadOnlyDictionary<string, ICtfTypeDescriptor> typeDeclarations)
            {
                this.AddedEvents.Add(new AddedEvent { Assignments = assignments, TypeDeclarations = typeDeclarations });
            }

            public void AddClock(ICtfClockDescriptor clockDescriptor) => this.clocks.Add(clockDescriptor);

            public void AddStream(ICtfStreamDescriptor streamDescriptor) => this.streams.Add(streamDescriptor);
        }

        private sealed class BytePacketReader
            : IPacketReader
        {
            private readonly Queue<byte> bytes;

            public BytePacketReader(params byte[] bytes)
            {
                this.bytes = new Queue<byte>(bytes);
            }

            public bool EndOfStream => this.bytes.Count == 0;

            public uint RemainingBufferedBitCount => (uint)this.bytes.Count * 8;

            public byte[] ReadBits(uint bitCount)
            {
                Assert.AreEqual(8u, bitCount);
                return new[] { this.bytes.Dequeue() };
            }

            public byte[] ReadString() => throw new NotSupportedException();

            public void Align(uint bitCount)
            {
            }
        }
    }
}
