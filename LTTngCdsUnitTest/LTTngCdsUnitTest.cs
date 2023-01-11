// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using LTTngCdsUnitTest.SourceDataCookers;
using Microsoft.Performance.SDK.Extensibility;
using Microsoft.Performance.SDK.Processing;
using Microsoft.Performance.Toolkit.Engine;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LTTngCdsUnitTest
{
    [TestClass]
    public class LTTngCdsUnitTest
    {
        public static bool IsTraceProcessed = false;
        public static object IsTraceProcessedLock = new object();

        private static RuntimeExecutionResults RuntimeExecutionResults;

        private static DataCookerPath LTTngEventDataCookerPath;

        public static void ProcessTrace()
        {
            lock (IsTraceProcessedLock)
            {
                if (!IsTraceProcessed)
                {
                    // Input data
                    string[] lttngData = { @"..\..\..\..\TestData\LTTng\lttng-kernel-trace.ctf" };
                    var lttngDataPath = new FileInfo(lttngData[0]);
                    Assert.IsTrue(lttngDataPath.Exists);

                    var runtime = Engine.Create(new FileDataSource(lttngDataPath.FullName));

                    // Enable our various types of data
                    var lttngDataCooker = new LTTngEventDataCooker();
                    LTTngEventDataCookerPath = lttngDataCooker.Path;
                    runtime.EnableCooker(LTTngEventDataCookerPath);

                    //
                    // Process our data.
                    //

                    RuntimeExecutionResults = runtime.Process();

                    IsTraceProcessed = true;
                }
            }
        }

        [TestMethod]
        public void TestLTTngEvent()
        {
            ProcessTrace();

            var eventData = RuntimeExecutionResults.QueryOutput<IReadOnlyList<LTTngEventWithContext>>(
                new DataOutputPath(
                    LTTngEventDataCookerPath,
                    nameof(LTTngEventDataCooker.Events)));

            Assert.IsTrue(eventData.Count == 936356);

            var lttngEvent3 = eventData[3];

            // Context
            Assert.IsTrue(lttngEvent3.LTTngContext.Clocks.Count == 1);
            Assert.IsTrue(lttngEvent3.LTTngContext.Clocks["monotonic"].Name == "monotonic");
            Assert.IsTrue(lttngEvent3.LTTngContext.Clocks["monotonic"].Frequency == 1000000000);
            Assert.IsTrue(lttngEvent3.LTTngContext.Clocks["monotonic"].Offset == 1565905093154690150);
            Assert.IsTrue(lttngEvent3.LTTngContext.CurrentCpu == 1);
            Assert.IsTrue(lttngEvent3.LTTngContext.Timestamp == 7120199554940);
            Assert.IsTrue(lttngEvent3.LTTngContext.TracerMajor == 9 &&
                          lttngEvent3.LTTngContext.TracerMinor == 10);

            // LTTngEvent
            Assert.IsTrue(lttngEvent3.LTTngEvent.Id == 1178);
            Assert.IsTrue(lttngEvent3.LTTngEvent.Name == "sched_waking");
            Assert.IsTrue(lttngEvent3.LTTngEvent.Payload.Fields.Count == 4);
            Assert.IsTrue(lttngEvent3.LTTngEvent.StreamDefinedEventContext.Fields.Count == 3);
            Assert.IsTrue(lttngEvent3.LTTngEvent.StreamDefinedEventContext.FieldsByName.ContainsKey("_tid"));
            Assert.IsTrue(lttngEvent3.LTTngEvent.StreamDefinedEventContext.FieldsByName.ContainsKey("_pid"));
            Assert.IsTrue(lttngEvent3.LTTngEvent.StreamDefinedEventContext.FieldsByName.ContainsKey("_procname"));
            Assert.IsTrue(lttngEvent3.LTTngEvent.WallClockTime == new DateTime(637015090133542450));
            Assert.IsTrue(lttngEvent3.LTTngEvent.Timestamp == new Microsoft.Performance.SDK.Timestamp(7120199554940));
        }
    }
}