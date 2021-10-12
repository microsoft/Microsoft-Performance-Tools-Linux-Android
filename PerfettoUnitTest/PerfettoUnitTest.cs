using Microsoft.Performance.SDK.Extensibility;
using Microsoft.Performance.SDK.Processing;
using Microsoft.Performance.Toolkit.Engine;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PerfettoCds;
using PerfettoCds.Pipeline.CompositeDataCookers;
using PerfettoCds.Pipeline.DataOutput;
using PerfettoCds.Pipeline.SourceDataCookers;
using PerfettoProcessor;
using System.IO;
using System.Linq;

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
                var runtime = Engine.Create(
                    new EngineCreateInfo()
                    {
                        LoggerFactory = new System.Func<System.Type, ILogger>((type) =>
                        { 
                            return new ConsoleLogger(type); 
                        }),
                    });

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
                runtime.EnableCooker(PerfettoPluginConstants.ProcessCounterTrackCookerPath);
                runtime.EnableCooker(PerfettoPluginConstants.CounterTrackCookerPath);
                runtime.EnableCooker(PerfettoPluginConstants.PerfSampleCookerPath);
                runtime.EnableCooker(PerfettoPluginConstants.StackProfileCallSiteCookerPath);
                runtime.EnableCooker(PerfettoPluginConstants.StackProfileFrameCookerPath);
                runtime.EnableCooker(PerfettoPluginConstants.StackProfileMappingCookerPath);
                runtime.EnableCooker(PerfettoPluginConstants.StackProfileSymbolCookerPath);

                // Enable the composite data cookers
                runtime.EnableCooker(PerfettoPluginConstants.GenericEventCookerPath);
                runtime.EnableCooker(PerfettoPluginConstants.CpuSchedEventCookerPath);
                runtime.EnableCooker(PerfettoPluginConstants.LogcatEventCookerPath);
                runtime.EnableCooker(PerfettoPluginConstants.FtraceEventCookerPath);
                runtime.EnableCooker(PerfettoPluginConstants.CpuFrequencyEventCookerPath);
                runtime.EnableCooker(PerfettoPluginConstants.CpuSamplingEventCookerPath);

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
            Assert.IsTrue(genericEventData[0].Thread == "TraceLogApiTest (20855)");
            Assert.IsTrue(genericEventData[0].Process == "TraceLogApiTest (20855)");

            var cpuSchedEventData = RuntimeExecutionResults.QueryOutput<ProcessedEventData<PerfettoCpuSchedEvent>>(
                new DataOutputPath(
                    PerfettoPluginConstants.CpuSchedEventCookerPath,
                    nameof(PerfettoCpuSchedEventCooker.CpuSchedEvents)));
            Assert.IsTrue(cpuSchedEventData.Count == 15267);
            Assert.IsTrue(cpuSchedEventData[0].ThreadName == "kworker/u17:9 (834)");
            Assert.IsTrue(cpuSchedEventData[1].EndState == "Task Dead");
            Assert.IsTrue(cpuSchedEventData[0].ProcessName == string.Empty);

            Assert.IsTrue(cpuSchedEventData[5801].EndState == "Runnable");
            Assert.IsTrue(cpuSchedEventData[5801].ThreadName == "TraceLogApiTest (20855)");
            Assert.IsTrue(cpuSchedEventData[5801].ProcessName == "TraceLogApiTest (20855)");

            var ftraceEventData = RuntimeExecutionResults.QueryOutput<ProcessedEventData<PerfettoFtraceEvent>>(
                new DataOutputPath(
                    PerfettoPluginConstants.FtraceEventCookerPath,
                    nameof(PerfettoFtraceEventCooker.FtraceEvents)));
            Assert.IsTrue(ftraceEventData.Count == 35877);
            Assert.IsTrue(ftraceEventData[0].ThreadName == "swapper (0)");
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
        public void TestAndroid12Trace()
        {
            // TODO - Get a smaller trace
            LoadTrace(@"..\..\..\..\TestData\Perfetto\perfetto_trace_cpu_sampling_not_scoped.pftrace");

            var perfSampleData = RuntimeExecutionResults.QueryOutput<ProcessedEventData<PerfettoPerfSampleEvent>>(
                new DataOutputPath(
                    PerfettoPluginConstants.PerfSampleCookerPath,
                    nameof(PerfettoPerfSampleCooker.PerfSampleEvents)));
            Assert.IsTrue(perfSampleData.Count >= 1);
            Assert.IsTrue(perfSampleData.Count == 684);
            Assert.IsTrue(perfSampleData[0].Cpu == 2);
            Assert.IsTrue(perfSampleData[0].CallsiteId == 32);
            Assert.IsTrue(perfSampleData[0].Utid == 3);
            Assert.IsTrue(perfSampleData[0].CpuMode == "kernel");
            Assert.IsTrue(perfSampleData[0].Type == "perf_sample");
            Assert.IsTrue(perfSampleData[0].Timestamp == 3958539411500);
            Assert.IsTrue(perfSampleData[0].RelativeTimestamp == 30446232358);

            var cpuSamplingData = RuntimeExecutionResults.QueryOutput<ProcessedEventData<PerfettoCpuSamplingEvent>>(
                new DataOutputPath(
                    PerfettoPluginConstants.CpuSamplingEventCookerPath,
                    nameof(PerfettoCpuSamplingEventCooker.CpuSamplingEvents)));
            Assert.IsTrue(cpuSamplingData.Count >= 1);
            Assert.IsTrue(cpuSamplingData.Count == 684);
            Assert.IsTrue(cpuSamplingData[0].Cpu == 2);
            Assert.IsTrue(cpuSamplingData[0].CpuMode == "kernel");
            Assert.IsTrue(cpuSamplingData[0].ProcessName == "/system/bin/traced_probes (446)");
            Assert.IsTrue(cpuSamplingData[0].ThreadName == "traced_probes (446)");
            Assert.IsTrue(cpuSamplingData[0].CallStack.Length == 33);
            Assert.IsTrue(cpuSamplingData[0].CallStack[0] == "/apex/com.android.runtime/lib64/bionic/libc.so!__libc_init");
            Assert.IsTrue(cpuSamplingData[0].CallStack[32] == "/kernel!smp_call_function_many_cond");

            //Assert.IsTrue(genericEventData[0].EventName == "Hello Trace");
            //Assert.IsTrue(genericEventData[0].Thread == "TraceLogApiTest (20855)");
            //Assert.IsTrue(genericEventData[0].Process == "TraceLogApiTest (20855)");
        }

        [TestMethod]
        public void TestAndroidMemoryTrace()
        {
            LoadTrace(@"..\..\..\..\TestData\Perfetto\androidBasicMemory.pftrace");

            var systemMemoryEventData = RuntimeExecutionResults.QueryOutput<ProcessedEventData<PerfettoSystemMemoryEvent>>(
                new DataOutputPath(
                    PerfettoPluginConstants.SystemMemoryEventCookerPath,
                    nameof(PerfettoSystemMemoryEventCooker.SystemMemoryEvents)));
            Assert.IsTrue(systemMemoryEventData.Count == 810);
            Assert.IsTrue(systemMemoryEventData[0].Value == 4008026112);
            Assert.IsTrue(systemMemoryEventData[1].Duration.ToNanoseconds == 249555208);

            var processMemoryEventData = RuntimeExecutionResults.QueryOutput<ProcessedEventData<PerfettoProcessMemoryEvent>>(
                new DataOutputPath(
                    PerfettoPluginConstants.ProcessMemoryEventCookerPath,
                    nameof(PerfettoProcessMemoryEventCooker.ProcessMemoryEvents)));
            Assert.IsTrue(processMemoryEventData.Count == 10811);
            Assert.IsTrue(processMemoryEventData[0].RssFile == 2822144);
            Assert.IsTrue(processMemoryEventData[1].ProcessName == "/system/bin/init 1");
        }

        [TestMethod]
        public void TestChromeTrace()
        {
            LoadTrace(@"..\..\..\..\TestData\Perfetto\chrome.pftrace");

            var genericEventData = RuntimeExecutionResults.QueryOutput<ProcessedEventData<PerfettoGenericEvent>>(
                new DataOutputPath(
                    PerfettoPluginConstants.GenericEventCookerPath,
                    nameof(PerfettoGenericEventCooker.GenericEvents)));
            Assert.IsTrue(genericEventData.Count == 147906);
            Assert.IsTrue(genericEventData[1].EventName == "PipelineReporter");
            Assert.IsTrue(genericEventData[1].Process == "Renderer (27768)");
            Assert.IsTrue(genericEventData[1].ParentId == null);
            Assert.IsTrue(genericEventData[1].ParentTreeDepthLevel == 0);
            Assert.IsTrue(genericEventData[1].ParentEventNameTree[0] == "[Root]");
            Assert.IsTrue(genericEventData[1].ParentEventNameTree[1] == "PipelineReporter");

            Assert.IsTrue(genericEventData[2].EventName == "BeginImplFrameToSendBeginMainFrame");
            Assert.IsTrue(genericEventData[2].Process == "Renderer (27768)");
            Assert.IsTrue(genericEventData[2].ParentId == 1);
            Assert.IsTrue(genericEventData[2].ParentTreeDepthLevel == 1);
            Assert.IsTrue(genericEventData[2].ParentEventNameTree[1] == "PipelineReporter");
            Assert.IsTrue(genericEventData[2].ParentEventNameTree[2] == "BeginImplFrameToSendBeginMainFrame");

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
