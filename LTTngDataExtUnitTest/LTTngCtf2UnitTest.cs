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
using LTTngDataExtensions.SourceDataCookers.Thread;
using LTTngDataExtensions.Tables;
using Microsoft.Performance.SDK.Extensibility;
using Microsoft.Performance.SDK.Processing;
using Microsoft.Performance.Toolkit.Engine;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LTTngDataExtUnitTest
{
    /// <summary>
    /// Processes a small synthetic LTTng kernel trace written in the CTF 2 format, with channels that are not named
    /// "chan*" and event ids that are only unique within a channel (as written by LTTng 2.15+).
    /// </summary>
    [TestClass]
    public class LTTngCtf2UnitTest
    {
        private const string Metadata =
            "\u001e{\"type\":\"preamble\",\"version\":2}\n" +
            "\u001e{\"type\":\"field-class-alias\",\"name\":\"u64-ts\",\"field-class\":{\"type\":\"fixed-length-unsigned-integer\",\"length\":64,\"alignment\":8,\"byte-order\":\"little-endian\",\"roles\":[\"default-clock-timestamp\"]}}\n" +
            "\u001e{\"type\":\"field-class-alias\",\"name\":\"u64\",\"field-class\":{\"type\":\"fixed-length-unsigned-integer\",\"length\":64,\"alignment\":8,\"byte-order\":\"little-endian\"}}\n" +
            "\u001e{\"type\":\"field-class-alias\",\"name\":\"u32\",\"field-class\":{\"type\":\"fixed-length-unsigned-integer\",\"length\":32,\"alignment\":8,\"byte-order\":\"little-endian\"}}\n" +
            "\u001e{\"type\":\"field-class-alias\",\"name\":\"s32\",\"field-class\":{\"type\":\"fixed-length-signed-integer\",\"length\":32,\"alignment\":8,\"byte-order\":\"little-endian\"}}\n" +
            "\u001e{\"type\":\"field-class-alias\",\"name\":\"comm\",\"field-class\":{\"type\":\"static-length-string\",\"length\":16}}\n" +
            "\u001e{\"type\":\"field-class-alias\",\"name\":\"er-header-compact\",\"field-class\":{\"type\":\"structure\",\"minimum-alignment\":8,\"member-classes\":[{\"name\":\"id\",\"field-class\":{\"type\":\"fixed-length-unsigned-integer\",\"length\":5,\"byte-order\":\"little-endian\",\"mappings\":{\"compact\":[[0,30]],\"extended\":[[31,31]]}}},{\"name\":\"v\",\"field-class\":{\"type\":\"variant\",\"selector-field-location\":{\"path\":[\"id\"]},\"options\":[{\"name\":\"compact\",\"selector-field-ranges\":[[0,30]],\"field-class\":{\"type\":\"structure\",\"member-classes\":[{\"name\":\"timestamp\",\"field-class\":{\"type\":\"fixed-length-unsigned-integer\",\"length\":27,\"byte-order\":\"little-endian\",\"roles\":[\"default-clock-timestamp\"]}}]}},{\"name\":\"extended\",\"selector-field-ranges\":[[31,31]],\"field-class\":{\"type\":\"structure\",\"member-classes\":[{\"name\":\"id\",\"field-class\":\"u32\"},{\"name\":\"timestamp\",\"field-class\":\"u64-ts\"}]}}]}}]}}\n" +
            "\u001e{\"type\":\"field-class-alias\",\"name\":\"pkt-ctx\",\"field-class\":{\"type\":\"structure\",\"member-classes\":[{\"name\":\"timestamp_begin\",\"field-class\":\"u64-ts\"},{\"name\":\"timestamp_end\",\"field-class\":{\"type\":\"fixed-length-unsigned-integer\",\"length\":64,\"alignment\":8,\"byte-order\":\"little-endian\",\"roles\":[\"packet-end-default-clock-timestamp\"]}},{\"name\":\"content_size\",\"field-class\":\"u64\"},{\"name\":\"packet_size\",\"field-class\":\"u64\"},{\"name\":\"events_discarded\",\"field-class\":\"u64\"},{\"name\":\"cpu_id\",\"field-class\":\"u32\"}]}}\n" +
            "\u001e{\"type\":\"field-class-alias\",\"name\":\"pkt-header\",\"field-class\":{\"type\":\"structure\",\"member-classes\":[{\"name\":\"magic\",\"field-class\":\"u32\"},{\"name\":\"uuid\",\"field-class\":{\"type\":\"static-length-blob\",\"length\":16}},{\"name\":\"stream_id\",\"field-class\":\"u32\"}]}}\n" +
            "\u001e{\"type\":\"trace-class\",\"environment\":{\"domain\":\"kernel\",\"tracer_name\":\"lttng-modules\",\"tracer_major\":2,\"tracer_minor\":16},\"packet-header-field-class\":\"pkt-header\"}\n" +
            "\u001e{\"type\":\"clock-class\",\"id\":\"monotonic\",\"name\":\"monotonic\",\"frequency\":1000000000,\"origin\":\"unix-epoch\",\"offset-from-origin\":{\"seconds\":1790000000,\"cycles\":0}}\n" +
            "\u001e{\"type\":\"data-stream-class\",\"id\":1,\"default-clock-class-id\":\"monotonic\",\"packet-context-field-class\":\"pkt-ctx\",\"event-record-header-field-class\":\"er-header-compact\",\"event-record-common-context-field-class\":{\"type\":\"structure\",\"member-classes\":[{\"name\":\"pid\",\"field-class\":\"s32\"},{\"name\":\"tid\",\"field-class\":\"s32\"},{\"name\":\"procname\",\"field-class\":\"comm\"},{\"name\":\"prio\",\"field-class\":\"s32\"}]}}\n" +
            "\u001e{\"type\":\"data-stream-class\",\"id\":0,\"default-clock-class-id\":\"monotonic\",\"packet-context-field-class\":\"pkt-ctx\",\"event-record-header-field-class\":\"er-header-compact\",\"event-record-common-context-field-class\":{\"type\":\"structure\",\"member-classes\":[{\"name\":\"perf_cpu_cycles\",\"field-class\":\"u64\"},{\"name\":\"perf_cpu_instructions\",\"field-class\":\"u64\"},{\"name\":\"pid\",\"field-class\":\"s32\"},{\"name\":\"tid\",\"field-class\":\"s32\"},{\"name\":\"procname\",\"field-class\":\"comm\"},{\"name\":\"prio\",\"field-class\":\"s32\"}]}}\n" +
            "\u001e{\"type\":\"event-record-class\",\"data-stream-class-id\":1,\"id\":0,\"name\":\"sched_wakeup\",\"payload-field-class\":{\"type\":\"structure\",\"member-classes\":[{\"name\":\"comm\",\"field-class\":\"comm\"},{\"name\":\"tid\",\"field-class\":\"s32\"},{\"name\":\"prio\",\"field-class\":\"s32\"},{\"name\":\"target_cpu\",\"field-class\":\"s32\"}]}}\n" +
            "\u001e{\"type\":\"event-record-class\",\"data-stream-class-id\":0,\"id\":0,\"name\":\"sched_switch\",\"payload-field-class\":{\"type\":\"structure\",\"member-classes\":[{\"name\":\"prev_comm\",\"field-class\":\"comm\"},{\"name\":\"prev_tid\",\"field-class\":\"s32\"},{\"name\":\"prev_prio\",\"field-class\":\"s32\"},{\"name\":\"prev_state\",\"field-class\":{\"type\":\"fixed-length-signed-integer\",\"length\":64,\"alignment\":8,\"byte-order\":\"little-endian\"}},{\"name\":\"next_comm\",\"field-class\":\"comm\"},{\"name\":\"next_tid\",\"field-class\":\"s32\"},{\"name\":\"next_prio\",\"field-class\":\"s32\"}]}}\n";

        private static string tracePath;
        private static RuntimeExecutionResults results;
        private static DataCookerPath threadCookerPath;
        private static DataCookerPath genericCookerPath;

        [ClassInitialize]
        public static void ProcessTrace(TestContext context)
        {
            tracePath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".ctf");
            WriteTrace(tracePath);

            var runtime = Engine.Create(new FileDataSource(tracePath));

            var threadCooker = new LTTngThreadDataCooker();
            threadCookerPath = threadCooker.Path;
            runtime.EnableCooker(threadCookerPath);

            var genericCooker = new LTTngGenericEventDataCooker();
            genericCookerPath = genericCooker.Path;
            runtime.EnableCooker(genericCookerPath);

            runtime.EnableTable(ExecutionEventTable.TableDescriptor);

            results = runtime.Process();
        }

        [ClassCleanup]
        public static void Cleanup()
        {
            if (tracePath != null && File.Exists(tracePath))
            {
                File.Delete(tracePath);
            }
        }

        [TestMethod]
        public void ProcessesAllChannels()
        {
            Assert.AreEqual(0, results.ProcessingErrors.Count(), string.Join(Environment.NewLine, results.ProcessingErrors.Select(e => e.ProcessFault?.ToString())));

            var events = results.QueryOutput<ProcessedEventData<LTTngGenericEvent>>(
                new DataOutputPath(genericCookerPath, nameof(LTTngGenericEventDataCooker.Events)));

            var names = Enumerable.Range(0, (int)events.Count).Select(i => events[i].EventName).ToList();
            Assert.AreEqual(3, names.Count(n => n == "sched_switch"));
            Assert.AreEqual(1, names.Count(n => n == "sched_wakeup"));

            var wakeup = Enumerable.Range(0, (int)events.Count).Select(i => events[i]).Single(e => e.EventName == "sched_wakeup");
            Assert.AreEqual("taskA", wakeup.FieldValues[wakeup.FieldNames.IndexOf("_comm")]);
        }

        [TestMethod]
        public void ExecutionEventsHavePerformanceCounterDeltas()
        {
            var executionEvents = results.QueryOutput<IReadOnlyList<IExecutionEvent>>(
                new DataOutputPath(threadCookerPath, nameof(LTTngThreadDataCooker.ExecutionEvents)));

            Assert.AreEqual(2, executionEvents.Count);

            var taskA = executionEvents.Single(e => e.NextTid == 100);
            Assert.AreEqual("taskA", taskA.NextImage);
            Assert.AreEqual(500, taskA.PerformanceCountersDiffByName["_perf_cpu_cycles"]);
            Assert.AreEqual(600, taskA.PerformanceCountersDiffByName["_perf_cpu_instructions"]);

            var idle = executionEvents.Single(e => e.NextTid == 0);
            Assert.AreEqual(300, idle.PerformanceCountersDiffByName["_perf_cpu_cycles"]);
            Assert.AreEqual(400, idle.PerformanceCountersDiffByName["_perf_cpu_instructions"]);

            var table = results.BuildTable(ExecutionEventTable.TableDescriptor);
            var cycles = table.Columns.Single(c => c.Configuration.Metadata.Name == "CPU Cycle");
            var instructions = table.Columns.Single(c => c.Configuration.Metadata.Name == "Instruction Count");
            CollectionAssert.AreEquivalent(new long[] { 500, 300 }, Enumerable.Range(0, table.RowCount).Select(i => (long)cycles.Project(i)).ToArray());
            CollectionAssert.AreEquivalent(new long[] { 600, 400 }, Enumerable.Range(0, table.RowCount).Select(i => (long)instructions.Project(i)).ToArray());
        }

        private static void WriteTrace(string path)
        {
            using (var archive = ZipFile.Open(path, ZipArchiveMode.Create))
            {
                WriteEntry(archive, "trace/kernel/metadata", Encoding.UTF8.GetBytes(Metadata));

                // Channel "sw-chan" (stream class 0) on CPU 0: idle -> taskA -> idle -> taskA.
                WriteEntry(archive, "trace/kernel/sw-chan_0", Packet(0, 1000, 3000,
                    SchedSwitch(1000, cycles: 1000, instructions: 2000, prevTid: 0, prevComm: "swapper/0", nextTid: 100, nextComm: "taskA"),
                    SchedSwitch(2000, cycles: 1500, instructions: 2600, prevTid: 100, prevComm: "taskA", nextTid: 0, nextComm: "swapper/0"),
                    SchedSwitch(3000, cycles: 1800, instructions: 3000, prevTid: 0, prevComm: "swapper/0", nextTid: 100, nextComm: "taskA")));

                // Channel "other-chan" (stream class 1) on CPU 0: its event id 0 is sched_wakeup, not sched_switch.
                WriteEntry(archive, "trace/kernel/other-chan_0", Packet(1, 1500, 1500, SchedWakeup(1500, tid: 100, comm: "taskA")));
            }
        }

        private static void WriteEntry(ZipArchive archive, string name, byte[] content)
        {
            using (var stream = archive.CreateEntry(name).Open())
            {
                stream.Write(content, 0, content.Length);
            }
        }

        private static byte[] Packet(uint streamId, ulong begin, ulong end, params byte[][] events)
        {
            const int headerAndContextSize = 4 + 16 + 4 + (5 * 8) + 4;
            ulong sizeInBits = (ulong)(headerAndContextSize + events.Sum(e => e.Length)) * 8;

            var stream = new MemoryStream();
            var writer = new BinaryWriter(stream);
            writer.Write(0xC1FC1FC1u);
            writer.Write(new byte[16]);
            writer.Write(streamId);
            writer.Write(begin);
            writer.Write(end);
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

        private static byte[] SchedSwitch(uint timestamp, ulong cycles, ulong instructions, int prevTid, string prevComm, int nextTid, string nextComm)
        {
            var stream = new MemoryStream();
            var writer = new BinaryWriter(stream);
            WriteCompactHeader(writer, 0, timestamp);
            writer.Write(cycles);
            writer.Write(instructions);
            WriteProcessContext(writer, prevTid, prevComm);
            writer.Write(Comm(prevComm));
            writer.Write(prevTid);
            writer.Write(20);
            writer.Write(0L);
            writer.Write(Comm(nextComm));
            writer.Write(nextTid);
            writer.Write(20);
            return stream.ToArray();
        }

        private static byte[] SchedWakeup(uint timestamp, int tid, string comm)
        {
            var stream = new MemoryStream();
            var writer = new BinaryWriter(stream);
            WriteCompactHeader(writer, 0, timestamp);
            WriteProcessContext(writer, 0, "swapper/0");
            writer.Write(Comm(comm));
            writer.Write(tid);
            writer.Write(20);
            writer.Write(0);
            return stream.ToArray();
        }

        private static void WriteCompactHeader(BinaryWriter writer, uint id, uint timestamp)
        {
            // 5-bit event id followed by a 27-bit timestamp, packed least significant bit first.
            writer.Write(id | (timestamp << 5));
        }

        private static void WriteProcessContext(BinaryWriter writer, int tid, string procname)
        {
            writer.Write(tid);
            writer.Write(tid);
            writer.Write(Comm(procname));
            writer.Write(20);
        }

        private static byte[] Comm(string value)
        {
            var bytes = new byte[16];
            Encoding.UTF8.GetBytes(value, 0, value.Length, bytes, 0);
            return bytes;
        }
    }
}
