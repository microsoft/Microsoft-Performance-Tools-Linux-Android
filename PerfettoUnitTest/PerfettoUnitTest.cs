using Microsoft.Performance.SDK.Extensibility;
using Microsoft.Performance.SDK.Processing;
using Microsoft.Performance.Toolkit.Engine;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PerfettoCds;
using PerfettoCds.Pipeline.DataCookers;
using PerfettoCds.Pipeline.DataOutput;
using System.IO;

namespace PerfettoUnitTest
{
    [TestClass]
    public class PerfettoUnitTest
    {
        public static bool IsTraceProcessed = false;
        public static object IsTraceProcessedLock = new object();

        private static RuntimeExecutionResults RuntimeExecutionResults;

        public PerfettoUnitTest()
        {
            LoadTrace(@"..\..\..\..\TestData\Perfetto\test_trace.perfetto-trace");
        }

        public static void LoadTrace(string traceFilename)
        {
            lock (IsTraceProcessedLock)
            {
                if (!IsTraceProcessed)
                {
                    // Input data
                    var perfettoDataPath = new FileInfo(traceFilename);
                    Assert.IsTrue(perfettoDataPath.Exists);

                    // Start the SDK engine
                    var runtime = Engine.Create();
                    runtime.AddFile(perfettoDataPath.FullName);

                    // Enable the source data cookers
                    runtime.EnableCooker(PerfettoPluginConstants.SliceCookerPath);
                    runtime.EnableCooker(PerfettoPluginConstants.ArgCookerPath);
                    runtime.EnableCooker(PerfettoPluginConstants.ThreadTrackCookerPath);
                    runtime.EnableCooker(PerfettoPluginConstants.ThreadCookerPath);
                    runtime.EnableCooker(PerfettoPluginConstants.ProcessCookerPath);
                    runtime.EnableCooker(PerfettoPluginConstants.SchedSliceCookerPath);
                    runtime.EnableCooker(PerfettoPluginConstants.RawCookerPath);
                    runtime.EnableCooker(PerfettoPluginConstants.CounterCookerPath);
                    runtime.EnableCooker(PerfettoPluginConstants.CpuCounterTrackCookerPath);

                    // Enable the composite data cookers
                    runtime.EnableCooker(PerfettoPluginConstants.GenericEventCookerPath);
                    runtime.EnableCooker(PerfettoPluginConstants.CpuSchedEventCookerPath);
                    runtime.EnableCooker(PerfettoPluginConstants.LogcatEventCookerPath);
                    runtime.EnableCooker(PerfettoPluginConstants.FtraceEventCookerPath);
                    runtime.EnableCooker(PerfettoPluginConstants.CpuFrequencyEventCookerPath);

                    // Process our data.
                    RuntimeExecutionResults = runtime.Process();
                    IsTraceProcessed = true;
                }
            }
        }

        /// <summary>
        /// PerfettoGenericEvents gather data from multiple source cookers. Valid PerfettoGenericEvents ensure
        /// that the cookers below it also worked successfully.
        /// </summary>
        [TestMethod]
        public void TestPerfettoGenericEvents()
        {
            var eventData = RuntimeExecutionResults.QueryOutput<ProcessedEventData<PerfettoGenericEvent>>(
                new DataOutputPath(
                    PerfettoPluginConstants.GenericEventCookerPath,
                    nameof(PerfettoGenericEventCooker.GenericEvents)));

            Assert.IsTrue(eventData.Count == 1);

            Assert.IsTrue(eventData[0].EventName == "Hello Trace");

            Assert.IsTrue(eventData[0].Thread == "TraceLogApiTest 20855");
        }

        [TestMethod]
        public void TestPerfettoCpuSchedEvents()
        {
            var eventData = RuntimeExecutionResults.QueryOutput<ProcessedEventData<PerfettoCpuSchedEvent>>(
                new DataOutputPath(
                    PerfettoPluginConstants.CpuSchedEventCookerPath,
                    nameof(PerfettoCpuSchedEventCooker.CpuSchedEvents)));

            Assert.IsTrue(eventData.Count == 15267);

            Assert.IsTrue(eventData[0].ThreadName == "kworker/u17:9");
            Assert.IsTrue(eventData[1].EndState == "Task Dead");
        }

        [TestMethod]
        public void TestPerfettoFtraceEvents()
        {
            var eventData = RuntimeExecutionResults.QueryOutput<ProcessedEventData<PerfettoFtraceEvent>>(
                new DataOutputPath(
                    PerfettoPluginConstants.FtraceEventCookerPath,
                    nameof(PerfettoFtraceEventCooker.FtraceEvents)));

            Assert.IsTrue(eventData.Count == 35877);

            Assert.IsTrue(eventData[0].ThreadName == "swapper");
            Assert.IsTrue(eventData[1].Cpu == 3);
        }

        [TestMethod]
        public void TestCpuFrequencyEvents()
        {
            var eventData = RuntimeExecutionResults.QueryOutput<ProcessedEventData<PerfettoCpuFrequencyEvent>>(
                new DataOutputPath(
                    PerfettoPluginConstants.CpuFrequencyEventCookerPath,
                    nameof(PerfettoCpuFrequencyEventCooker.CpuFrequencyEvents)));

            Assert.IsTrue(eventData.Count == 11855);

            Assert.IsTrue(eventData[0].CpuNum == 3);
            Assert.IsTrue(eventData[1].Name == "cpuidle");
        }
    }
}
