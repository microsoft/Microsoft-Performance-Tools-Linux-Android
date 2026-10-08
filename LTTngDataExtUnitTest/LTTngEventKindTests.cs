// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
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
        private const string ValuePayload = @"[{""name"":""value"",""field-class"":""s32""}]";

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

        // Each trace has one event record class (id 0) and two events of it.
        private static List<string> Process(params (string Directory, string EventName, uint Timestamp, int Value)[] traces)
        {
            var events = Ctf2TestTraces.ProcessGenericEvents(traces.Select(trace => (
                trace.Directory,
                Ctf2TestTraces.Metadata("ust", Ctf2TestTraces.EventRecordClass(0, trace.EventName, ValuePayload)),
                Ctf2TestTraces.Packet(
                    trace.Timestamp,
                    trace.Timestamp + 100,
                    Ctf2TestTraces.Event(0, trace.Timestamp, BitConverter.GetBytes(trace.Value)),
                    Ctf2TestTraces.Event(0, trace.Timestamp + 100, BitConverter.GetBytes(trace.Value))))).ToArray());

            return events.Select(e => $"{e.EventName} = {e.FieldValues[e.FieldNames.IndexOf("_value")]}").ToList();
        }
    }
}