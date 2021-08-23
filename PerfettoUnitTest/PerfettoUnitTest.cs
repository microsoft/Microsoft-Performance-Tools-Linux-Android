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
        public static object IsTraceProcessedLock = new object();

        private static RuntimeExecutionResults RuntimeExecutionResults;

        public static void LoadTrace(string traceFilename)
        {
            lock (IsTraceProcessedLock)
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
            }
        }

        [TestMethod]
        public void TestAndroidTrace()
        {
            LoadTrace(@"..\..\..\..\TestData\Perfetto\test_trace.perfetto-trace");

            var genericEventData = RuntimeExecutionResults.QueryOutput<ProcessedEventData<PerfettoGenericEvent>>(
                new DataOutputPath(
                    PerfettoPluginConstants.GenericEventCookerPath,
                    nameof(PerfettoGenericEventCooker.GenericEvents)));
            Assert.IsTrue(genericEventData.Count == 1);
            Assert.IsTrue(genericEventData[0].EventName == "Hello Trace");
            Assert.IsTrue(genericEventData[0].Thread == "TraceLogApiTest 20855");

            var cpuSchedEventData = RuntimeExecutionResults.QueryOutput<ProcessedEventData<PerfettoCpuSchedEvent>>(
                new DataOutputPath(
                    PerfettoPluginConstants.CpuSchedEventCookerPath,
                    nameof(PerfettoCpuSchedEventCooker.CpuSchedEvents)));
            Assert.IsTrue(cpuSchedEventData.Count == 15267);
            Assert.IsTrue(cpuSchedEventData[0].ThreadName == "kworker/u17:9");
            Assert.IsTrue(cpuSchedEventData[1].EndState == "Task Dead");

            var ftraceEventData = RuntimeExecutionResults.QueryOutput<ProcessedEventData<PerfettoFtraceEvent>>(
                new DataOutputPath(
                    PerfettoPluginConstants.FtraceEventCookerPath,
                    nameof(PerfettoFtraceEventCooker.FtraceEvents)));
            Assert.IsTrue(ftraceEventData.Count == 35877);
            Assert.IsTrue(ftraceEventData[0].ThreadName == "swapper");
            Assert.IsTrue(ftraceEventData[1].Cpu == 3);

            var cpuFreqEventData = RuntimeExecutionResults.QueryOutput<ProcessedEventData<PerfettoCpuFrequencyEvent>>(
                new DataOutputPath(
                    PerfettoPluginConstants.CpuFrequencyEventCookerPath,
                    nameof(PerfettoCpuFrequencyEventCooker.CpuFrequencyEvents)));
            Assert.IsTrue(cpuFreqEventData.Count == 11855);
            Assert.IsTrue(cpuFreqEventData[0].CpuNum == 3);
            Assert.IsTrue(cpuFreqEventData[1].Name == "cpuidle");
        }

        [TestMethod]
        public void TestChromeTrace()
        {
            LoadTrace(@"..\..\..\..\TestData\Perfetto\chrome.pftrace");

            var genericEventData = RuntimeExecutionResults.QueryOutput<ProcessedEventData<PerfettoGenericEvent>>(
                new DataOutputPath(
                    PerfettoPluginConstants.GenericEventCookerPath,
                    nameof(PerfettoGenericEventCooker.GenericEvents)));
            Assert.IsTrue(genericEventData.Count == 144194);
            Assert.IsTrue(genericEventData[0].EventName == "WebContentsImpl::DidReceiveInputEvent");
            Assert.IsTrue(genericEventData[0].Thread == "CrBrowserMain 14448");

            var logcatEventData = RuntimeExecutionResults.QueryOutput<ProcessedEventData<PerfettoLogcatEvent>>(
                new DataOutputPath(
                    PerfettoPluginConstants.LogcatEventCookerPath,
                    nameof(PerfettoLogcatEventCooker.LogcatEvents)));
            Assert.IsTrue(logcatEventData.Count == 43);
            Assert.IsTrue(logcatEventData[0].Message == "type: 97 score: 0.8\n");
            Assert.IsTrue(logcatEventData[1].ProcessName == "Browser");
        }
    }
}
