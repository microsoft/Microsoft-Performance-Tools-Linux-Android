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
    /// Builds small synthetic CTF 2 traces with one data stream class (id 0), and processes them.
    /// </summary>
    internal static class Ctf2TestTraces
    {
        internal const string StreamFileName = "channel0_0";

        /// <summary>
        /// Metadata with field class aliases u8, u16, u32, s32 and u64, and the given event record classes.
        /// </summary>
        internal static string Metadata(string domain, params string[] eventRecordClasses)
        {
            var fragments = new List<string>
            {
                "{\"type\":\"preamble\",\"version\":2}",
                "{\"type\":\"field-class-alias\",\"name\":\"u64-ts\",\"field-class\":{\"type\":\"fixed-length-unsigned-integer\",\"length\":64,\"alignment\":8,\"byte-order\":\"little-endian\",\"roles\":[\"default-clock-timestamp\"]}}",
                Integer("u8", 8, false),
                Integer("u16", 16, false),
                Integer("u32", 32, false),
                Integer("s32", 32, true),
                Integer("u64", 64, false),
                "{\"type\":\"field-class-alias\",\"name\":\"er-header-compact\",\"field-class\":{\"type\":\"structure\",\"minimum-alignment\":8,\"member-classes\":[{\"name\":\"id\",\"field-class\":{\"type\":\"fixed-length-unsigned-integer\",\"length\":5,\"byte-order\":\"little-endian\"}},{\"name\":\"v\",\"field-class\":{\"type\":\"variant\",\"selector-field-location\":{\"path\":[\"id\"]},\"options\":[{\"name\":\"compact\",\"selector-field-ranges\":[[0,30]],\"field-class\":{\"type\":\"structure\",\"member-classes\":[{\"name\":\"timestamp\",\"field-class\":{\"type\":\"fixed-length-unsigned-integer\",\"length\":27,\"byte-order\":\"little-endian\",\"roles\":[\"default-clock-timestamp\"]}}]}},{\"name\":\"extended\",\"selector-field-ranges\":[[31,31]],\"field-class\":{\"type\":\"structure\",\"member-classes\":[{\"name\":\"id\",\"field-class\":\"u32\"},{\"name\":\"timestamp\",\"field-class\":\"u64-ts\"}]}}]}}]}}",
                "{\"type\":\"trace-class\",\"environment\":{\"domain\":\"" + domain + "\"},\"packet-header-field-class\":{\"type\":\"structure\",\"member-classes\":[{\"name\":\"magic\",\"field-class\":\"u32\"},{\"name\":\"uuid\",\"field-class\":{\"type\":\"static-length-blob\",\"length\":16}},{\"name\":\"stream_id\",\"field-class\":\"u32\"}]}}",
                "{\"type\":\"clock-class\",\"id\":\"monotonic\",\"name\":\"monotonic\",\"frequency\":1000000000,\"origin\":\"unix-epoch\",\"offset-from-origin\":{\"seconds\":1790000000}}",
                "{\"type\":\"data-stream-class\",\"id\":0,\"default-clock-class-id\":\"monotonic\",\"event-record-header-field-class\":\"er-header-compact\",\"packet-context-field-class\":{\"type\":\"structure\",\"member-classes\":[{\"name\":\"timestamp_begin\",\"field-class\":\"u64-ts\"},{\"name\":\"timestamp_end\",\"field-class\":{\"type\":\"fixed-length-unsigned-integer\",\"length\":64,\"alignment\":8,\"byte-order\":\"little-endian\",\"roles\":[\"packet-end-default-clock-timestamp\"]}},{\"name\":\"content_size\",\"field-class\":\"u64\"},{\"name\":\"packet_size\",\"field-class\":\"u64\"},{\"name\":\"events_discarded\",\"field-class\":\"u64\"},{\"name\":\"cpu_id\",\"field-class\":\"u32\"}]}}",
            };

            fragments.AddRange(eventRecordClasses);
            return string.Concat(fragments.Select(fragment => "\u001e" + fragment + "\n"));
        }

        /// <summary>
        /// An event record class of data stream class 0. Members are JSON arrays of structure member classes.
        /// </summary>
        internal static string EventRecordClass(uint id, string name, string payloadMembers, string specificContextMembers = null)
        {
            string specificContext = specificContextMembers == null
                ? string.Empty
                : ",\"specific-context-field-class\":{\"type\":\"structure\",\"member-classes\":" + specificContextMembers + "}";

            return "{\"type\":\"event-record-class\",\"data-stream-class-id\":0,\"id\":" + id + ",\"name\":\"" + name + "\"" +
                specificContext +
                ",\"payload-field-class\":{\"type\":\"structure\",\"member-classes\":" + payloadMembers + "}}";
        }

        /// <summary>
        /// An event with a compact header (5-bit event record class id, then a 27-bit timestamp), followed by the
        /// given context and payload bytes.
        /// </summary>
        internal static byte[] Event(uint id, uint timestamp, params byte[][] fields)
        {
            Assert.IsTrue(id < 31 && timestamp < (1u << 27));
            return BitConverter.GetBytes(id | (timestamp << 5)).Concat(fields.SelectMany(field => field)).ToArray();
        }

        /// <summary>
        /// A packet of data stream 0 with the given events.
        /// </summary>
        internal static byte[] Packet(uint timestampBegin, uint timestampEnd, params byte[][] events)
        {
            const int headerAndContextBytes = 4 + 16 + 4 + (5 * 8) + 4;
            ulong sizeInBits = (ulong)(headerAndContextBytes + events.Sum(e => e.Length)) * 8;

            var stream = new MemoryStream();
            var writer = new BinaryWriter(stream);
            writer.Write(0xC1FC1FC1u);
            writer.Write(new byte[16]);
            writer.Write(0u);
            writer.Write((ulong)timestampBegin);
            writer.Write((ulong)timestampEnd);
            writer.Write(sizeInBits);
            writer.Write(sizeInBits);
            writer.Write(0ul);
            writer.Write(0u);
            foreach (var e in events)
            {
                writer.Write(e);
            }

            return stream.ToArray();
        }

        /// <summary>
        /// Zips the traces (each a metadata file and one stream file in its directory), processes the archive, and
        /// returns the generic events.
        /// </summary>
        internal static List<LTTngGenericEvent> ProcessGenericEvents(params (string Directory, string Metadata, byte[] Stream)[] traces)
        {
            string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".ctf");
            try
            {
                using (var archive = ZipFile.Open(path, ZipArchiveMode.Create))
                {
                    foreach (var trace in traces)
                    {
                        WriteEntry(archive, trace.Directory + "/metadata", Encoding.UTF8.GetBytes(trace.Metadata));
                        WriteEntry(archive, trace.Directory + "/" + StreamFileName, trace.Stream);
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

                    return Enumerable.Range(0, (int)events.Count).Select(i => events[i]).ToList();
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

        private static string Integer(string alias, int length, bool signed)
        {
            return "{\"type\":\"field-class-alias\",\"name\":\"" + alias + "\",\"field-class\":{\"type\":\"fixed-length-" + (signed ? "signed" : "unsigned") +
                "-integer\",\"length\":" + length + ",\"alignment\":8,\"byte-order\":\"little-endian\"}}";
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
