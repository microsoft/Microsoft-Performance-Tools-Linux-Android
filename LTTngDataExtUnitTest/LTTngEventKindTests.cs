// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using LTTngDataExtensions.DataOutputTypes;
using LTTngDataExtensions.SourceDataCookers;
using Microsoft.Performance.SDK.Extensibility;
using Microsoft.Performance.SDK.Processing;
using Microsoft.Performance.Toolkit.Engine;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LTTngDataExtUnitTest
{
    /// <summary>
    /// Generic events must be named from their own trace's metadata, even when traces share a domain and their
    /// stream and event ids overlap (e.g. per-UID and per-PID UST buffers), and when traces are processed one after
    /// another in the same process.
    /// </summary>
    [TestClass]
    public class LTTngEventKindTests
    {
        [TestMethod]
        public void TracesInOneInputWithOverlappingEventIds()
        {
            var events = Process(
                ("trace/ust/uid/1000/64-bit", "app_a_event", 1000, 1),
                ("trace/ust/pid/app-42/64-bit", "app_b_event", 2000, 2));

            CollectionAssert.AreEquivalent(new[] { "app_a_event = 1", "app_a_event = 1", "app_b_event = 2", "app_b_event = 2" }, events, string.Join(", ", events));
        }

        [TestMethod]
        public void TracesProcessedOneAfterAnother()
        {
            var first = Process(("trace/ust/uid/1000/64-bit", "first_event", 1000, 1));
            var second = Process(("trace/ust/uid/1000/64-bit", "second_event", 1000, 2));

            CollectionAssert.AreEqual(new[] { "first_event = 1", "first_event = 1" }, first, string.Join(", ", first));
            CollectionAssert.AreEqual(new[] { "second_event = 2", "second_event = 2" }, second, string.Join(", ", second));
        }

        private static List<string> Process(params (string Directory, string EventName, uint Timestamp, int Value)[] traces)
        {
            string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".ctf");
            try
            {
                using (var archive = ZipFile.Open(path, ZipArchiveMode.Create))
                {
                    foreach (var trace in traces)
                    {
                        WriteEntry(archive, trace.Directory + "/metadata", Encoding.UTF8.GetBytes(Metadata("ust", trace.EventName)));
                        WriteEntry(archive, trace.Directory + "/channel0_0", Packet(trace.Timestamp, trace.Value));
                    }
                }

                using (var runtime = Engine.Create(new FileDataSource(path)))
                {
                    var cooker = new LTTngGenericEventDataCooker();
                    runtime.EnableCooker(cooker.Path);
                    var results = runtime.Process();

                    Assert.AreEqual(0, results.ProcessingErrors.Count(), string.Join(Environment.NewLine, results.ProcessingErrors.Select(e => e.ProcessFault?.ToString())));

                    var events = results.QueryOutput<ProcessedEventData<LTTngGenericEvent>>(
                        new DataOutputPath(cooker.Path, nameof(LTTngGenericEventDataCooker.Events)));

                    return Enumerable.Range(0, (int)events.Count)
                        .Select(i => events[i])
                        .Select(e => $"{e.EventName} = {e.FieldValues[e.FieldNames.IndexOf("_value")]}")
                        .ToList();
                }
            }
            finally
            {
                // The processor may still hold the archive open after the run.
                try
                {
                    File.Delete(path);
                }
                catch (IOException)
                {
                }
            }
        }

        // One data stream class (id 0) with one event record class (id 0) carrying a single integer.
        private static string Metadata(string domain, string eventName)
        {
            return
                "\u001e{\"type\":\"preamble\",\"version\":2}\n" +
                "\u001e{\"type\":\"field-class-alias\",\"name\":\"u64-ts\",\"field-class\":{\"type\":\"fixed-length-unsigned-integer\",\"length\":64,\"alignment\":8,\"byte-order\":\"little-endian\",\"roles\":[\"default-clock-timestamp\"]}}\n" +
                "\u001e{\"type\":\"field-class-alias\",\"name\":\"u64\",\"field-class\":{\"type\":\"fixed-length-unsigned-integer\",\"length\":64,\"alignment\":8,\"byte-order\":\"little-endian\"}}\n" +
                "\u001e{\"type\":\"field-class-alias\",\"name\":\"u32\",\"field-class\":{\"type\":\"fixed-length-unsigned-integer\",\"length\":32,\"alignment\":8,\"byte-order\":\"little-endian\"}}\n" +
                "\u001e{\"type\":\"field-class-alias\",\"name\":\"er-header-compact\",\"field-class\":{\"type\":\"structure\",\"minimum-alignment\":8,\"member-classes\":[{\"name\":\"id\",\"field-class\":{\"type\":\"fixed-length-unsigned-integer\",\"length\":5,\"byte-order\":\"little-endian\"}},{\"name\":\"v\",\"field-class\":{\"type\":\"variant\",\"selector-field-location\":{\"path\":[\"id\"]},\"options\":[{\"name\":\"compact\",\"selector-field-ranges\":[[0,30]],\"field-class\":{\"type\":\"structure\",\"member-classes\":[{\"name\":\"timestamp\",\"field-class\":{\"type\":\"fixed-length-unsigned-integer\",\"length\":27,\"byte-order\":\"little-endian\",\"roles\":[\"default-clock-timestamp\"]}}]}},{\"name\":\"extended\",\"selector-field-ranges\":[[31,31]],\"field-class\":{\"type\":\"structure\",\"member-classes\":[{\"name\":\"id\",\"field-class\":\"u32\"},{\"name\":\"timestamp\",\"field-class\":\"u64-ts\"}]}}]}}]}}\n" +
                "\u001e{\"type\":\"trace-class\",\"environment\":{\"domain\":\"" + domain + "\",\"tracer_name\":\"lttng-ust\"},\"packet-header-field-class\":{\"type\":\"structure\",\"member-classes\":[{\"name\":\"magic\",\"field-class\":\"u32\"},{\"name\":\"uuid\",\"field-class\":{\"type\":\"static-length-blob\",\"length\":16}},{\"name\":\"stream_id\",\"field-class\":\"u32\"}]}}\n" +
                "\u001e{\"type\":\"clock-class\",\"id\":\"monotonic\",\"name\":\"monotonic\",\"frequency\":1000000000,\"origin\":\"unix-epoch\",\"offset-from-origin\":{\"seconds\":1790000000}}\n" +
                "\u001e{\"type\":\"data-stream-class\",\"id\":0,\"default-clock-class-id\":\"monotonic\",\"event-record-header-field-class\":\"er-header-compact\",\"packet-context-field-class\":{\"type\":\"structure\",\"member-classes\":[{\"name\":\"timestamp_begin\",\"field-class\":\"u64-ts\"},{\"name\":\"timestamp_end\",\"field-class\":{\"type\":\"fixed-length-unsigned-integer\",\"length\":64,\"alignment\":8,\"byte-order\":\"little-endian\",\"roles\":[\"packet-end-default-clock-timestamp\"]}},{\"name\":\"content_size\",\"field-class\":\"u64\"},{\"name\":\"packet_size\",\"field-class\":\"u64\"},{\"name\":\"events_discarded\",\"field-class\":\"u64\"},{\"name\":\"cpu_id\",\"field-class\":\"u32\"}]}}\n" +
                "\u001e{\"type\":\"event-record-class\",\"data-stream-class-id\":0,\"id\":0,\"name\":\"" + eventName + "\",\"payload-field-class\":{\"type\":\"structure\",\"member-classes\":[{\"name\":\"value\",\"field-class\":{\"type\":\"fixed-length-signed-integer\",\"length\":32,\"alignment\":8,\"byte-order\":\"little-endian\"}}]}}\n";
        }

        // A packet of stream 0 holding two events (id 0), at the given timestamp and 100 ns later.
        private static byte[] Packet(uint timestamp, int value)
        {
            const int packetBytes = 4 + 16 + 4 + (5 * 8) + 4 + (2 * (4 + 4));
            ulong sizeInBits = packetBytes * 8;

            var stream = new MemoryStream();
            var writer = new BinaryWriter(stream);
            writer.Write(0xC1FC1FC1u);
            writer.Write(new byte[16]);
            writer.Write(0u);
            writer.Write((ulong)timestamp);
            writer.Write((ulong)timestamp + 100);
            writer.Write(sizeInBits);
            writer.Write(sizeInBits);
            writer.Write(0ul);
            writer.Write(0u);

            // Compact event header: 5-bit event id (0) followed by a 27-bit timestamp.
            foreach (uint eventTimestamp in new[] { timestamp, timestamp + 100 })
            {
                writer.Write(eventTimestamp << 5);
                writer.Write(value);
            }

            Assert.AreEqual(packetBytes, stream.Length);
            return stream.ToArray();
        }

        private static void WriteEntry(ZipArchive archive, string name, byte[] content)
        {
            using (var stream = archive.CreateEntry(name).Open())
            {
                stream.Write(content, 0, content.Length);
            }
        }
    }
}
