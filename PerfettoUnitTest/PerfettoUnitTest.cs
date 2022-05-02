using System.Collections.Generic;
using System.IO;
using Microsoft.Performance.SDK;
using Microsoft.Performance.SDK.Extensibility;
using Microsoft.Performance.SDK.Processing;
using Microsoft.Performance.Toolkit.Engine;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PerfettoCds;
using PerfettoCds.Pipeline.CompositeDataCookers;
using PerfettoCds.Pipeline.DataOutput;
using PerfettoCds.Pipeline.SourceDataCookers;
using PerfettoCds.Pipeline.Tables;
using PerfettoProcessor;
using UnitTestCommon;

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

                var dataSources = DataSourceSet.Create();
                dataSources.AddDataSource(new FileDataSource(perfettoDataPath.FullName));

                // Start the SDK engine
                var runtime = Engine.Create(
                    new EngineCreateInfo(dataSources.AsReadOnly())
                    {
                        LoggerFactory = new System.Func<System.Type, ILogger>((type) =>
                        {
                            return new ConsoleLogger(type);
                        }),
                    });

                // Enable the source data cookers
                runtime.EnableCooker(PerfettoPluginConstants.SliceCookerPath);
                runtime.EnableCooker(PerfettoPluginConstants.ArgCookerPath);
                runtime.EnableCooker(PerfettoPluginConstants.ThreadTrackCookerPath);
                runtime.EnableCooker(PerfettoPluginConstants.ThreadCookerPath);
                runtime.EnableCooker(PerfettoPluginConstants.ProcessRawCookerPath);
                runtime.EnableCooker(PerfettoPluginConstants.SchedSliceCookerPath);
                runtime.EnableCooker(PerfettoPluginConstants.RawCookerPath);
                runtime.EnableCooker(PerfettoPluginConstants.CounterCookerPath);
                runtime.EnableCooker(PerfettoPluginConstants.CpuCounterTrackCookerPath);
                runtime.EnableCooker(PerfettoPluginConstants.GpuCounterTrackCookerPath);
                runtime.EnableCooker(PerfettoPluginConstants.ProcessCounterTrackCookerPath);
                runtime.EnableCooker(PerfettoPluginConstants.CounterTrackCookerPath);
                runtime.EnableCooker(PerfettoPluginConstants.PerfSampleCookerPath);
                runtime.EnableCooker(PerfettoPluginConstants.StackProfileCallSiteCookerPath);
                runtime.EnableCooker(PerfettoPluginConstants.StackProfileFrameCookerPath);
                runtime.EnableCooker(PerfettoPluginConstants.StackProfileMappingCookerPath);
                runtime.EnableCooker(PerfettoPluginConstants.StackProfileSymbolCookerPath);
                runtime.EnableCooker(PerfettoPluginConstants.ExpectedFrameCookerPath);
                runtime.EnableCooker(PerfettoPluginConstants.ActualFrameCookerPath);
                runtime.EnableCooker(PerfettoPluginConstants.PackageListCookerPath);

                // Enable the composite data cookers
                runtime.EnableCooker(PerfettoPluginConstants.GenericEventCookerPath);
                runtime.EnableCooker(PerfettoPluginConstants.ProcessEventCookerPath);
                runtime.EnableCooker(PerfettoPluginConstants.GpuCountersEventCookerPath);
                runtime.EnableCooker(PerfettoPluginConstants.CpuSchedEventCookerPath);
                runtime.EnableCooker(PerfettoPluginConstants.LogcatEventCookerPath);
                runtime.EnableCooker(PerfettoPluginConstants.FtraceEventCookerPath);
                runtime.EnableCooker(PerfettoPluginConstants.CpuFrequencyEventCookerPath);
                runtime.EnableCooker(PerfettoPluginConstants.CpuSamplingEventCookerPath);
                runtime.EnableCooker(PerfettoPluginConstants.FrameEventCookerPath);

                // Enable tables used by UI
                runtime.EnableTable(PerfettoCpuCountersTable.TableDescriptor);
                runtime.EnableTable(PerfettoCpuFrequencyTable.TableDescriptor);
                runtime.EnableTable(PerfettoCpuSamplingTable.TableDescriptor);
                runtime.EnableTable(PerfettoCpuSchedTable.TableDescriptor);
                runtime.EnableTable(PerfettoFrameTable.TableDescriptor);
                runtime.EnableTable(PerfettoFtraceEventTable.TableDescriptor);
                runtime.EnableTable(PerfettoGenericEventTable.TableDescriptor);
                runtime.EnableTable(PerfettoGpuCountersTable.TableDescriptor);
                runtime.EnableTable(PerfettoLogcatEventTable.TableDescriptor);
                runtime.EnableTable(PerfettoPackageTable.TableDescriptor);
                runtime.EnableTable(PerfettoProcessMemoryTable.TableDescriptor);
                runtime.EnableTable(PerfettoProcessTable.TableDescriptor);
                runtime.EnableTable(PerfettoSystemMemoryTable.TableDescriptor);

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
            UnitTest.TestTableBuild(RuntimeExecutionResults, PerfettoGenericEventTable.TableDescriptor, 1);

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

            UnitTest.TestTableBuild(RuntimeExecutionResults, PerfettoCpuSchedTable.TableDescriptor, 15267);

            // Wake event validation
            Assert.IsTrue(cpuSchedEventData[0].WakeEvent.WokenTid == cpuSchedEventData[0].Tid);
            Assert.IsTrue(cpuSchedEventData[1].WakeEvent.WakerTid == 834);
            Assert.IsTrue(cpuSchedEventData[1].WakeEvent.WakerThreadName == "kworker/u17:9");
            Assert.IsTrue(cpuSchedEventData[9581].WakeEvent.WokenTid == cpuSchedEventData[9581].Tid);
            Assert.IsTrue(cpuSchedEventData[9581].WakeEvent.WakerTid == 19701);
            Assert.IsTrue(cpuSchedEventData[9581].WakeEvent.WakerThreadName == "kworker/u16:13");

            // Previous scheduling event validation
            Assert.IsTrue(cpuSchedEventData[9581].PreviousSchedulingEvent.EndState == "Task Dead");
            Assert.IsTrue(cpuSchedEventData[9581].PreviousSchedulingEvent.Tid == cpuSchedEventData[9581].Tid);

            var ftraceEventData = RuntimeExecutionResults.QueryOutput<ProcessedEventData<PerfettoFtraceEvent>>(
                new DataOutputPath(
                    PerfettoPluginConstants.FtraceEventCookerPath,
                    nameof(PerfettoFtraceEventCooker.FtraceEvents)));
            Assert.IsTrue(ftraceEventData.Count == 35877);
            Assert.IsTrue(ftraceEventData[0].ThreadFormattedName == "swapper (0)");
            Assert.IsTrue(ftraceEventData[1].Cpu == 3);
            UnitTest.TestTableBuild(RuntimeExecutionResults, PerfettoFtraceEventTable.TableDescriptor, 35877);

            var cpuFreqEventData = RuntimeExecutionResults.QueryOutput<ProcessedEventData<PerfettoCpuFrequencyEvent>>(
                new DataOutputPath(
                    PerfettoPluginConstants.CpuFrequencyEventCookerPath,
                    nameof(PerfettoCpuFrequencyEventCooker.CpuFrequencyEvents)));
            Assert.IsTrue(cpuFreqEventData.Count == 11855);
            Assert.IsTrue(cpuFreqEventData[0].CpuNum == 3);
            Assert.IsTrue(cpuFreqEventData[1].Name == "cpuidle");
            UnitTest.TestTableBuild(RuntimeExecutionResults, PerfettoCpuFrequencyTable.TableDescriptor, 11855);
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
            UnitTest.TestTableBuild(RuntimeExecutionResults, PerfettoCpuSamplingTable.TableDescriptor, 684);

            // Processes
            var processEventData = RuntimeExecutionResults.QueryOutput<ProcessedEventData<PerfettoProcessEvent>>(
                new DataOutputPath(
                    PerfettoPluginConstants.ProcessEventCookerPath,
                    nameof(PerfettoProcessEventCooker.ProcessEvents)));

            Assert.IsTrue(processEventData.Count == 121);
            Assert.IsTrue(processEventData[1].AndroidAppId == 10135);
            Assert.IsTrue(processEventData[1].Uid == 10135);
            Assert.IsTrue(processEventData[1].CmdLine == "com.android.systemui");
            Assert.IsTrue(processEventData[1].ParentUpid == 25);
            Assert.IsTrue(processEventData[1].ParentProcess != null && processEventData[1].ParentProcess.Name == "zygote64");
            Assert.IsTrue(processEventData[1].Pid == 980);
            Assert.IsTrue(processEventData[1].Upid == 1);
            Assert.IsTrue(processEventData[1].StartTimestamp == Timestamp.Zero); // NULL should be at trace start
            Assert.IsTrue(processEventData[1].EndTimestamp == new Timestamp(39446647558)); // NULL should be at trace stop

            Assert.IsTrue(processEventData[119].StartTimestamp == new Timestamp(33970357558));
            Assert.IsTrue(processEventData[119].EndTimestamp == new Timestamp(34203203358));
            Assert.IsTrue(processEventData[119].ParentProcess != null && processEventData[119].ParentProcess.Name == "/apex/com.android.adbd/bin/adbd");
            UnitTest.TestTableBuild(RuntimeExecutionResults, PerfettoProcessTable.TableDescriptor, 121);

            // Packages
            var packagesList = RuntimeExecutionResults.QueryOutput<ProcessedEventData<PerfettoPackageListEvent>>(
                new DataOutputPath(
                    PerfettoPluginConstants.PackageListCookerPath,
                    nameof(PerfettoPackageListCooker.PackageListEvents)));
            Assert.IsTrue(packagesList.Count == 0);
            UnitTest.TestTableBuild(RuntimeExecutionResults, PerfettoPackageTable.TableDescriptor, 0, true);          
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
            UnitTest.TestTableBuild(RuntimeExecutionResults, PerfettoSystemMemoryTable.TableDescriptor, 810);

            var processMemoryEventData = RuntimeExecutionResults.QueryOutput<ProcessedEventData<PerfettoProcessMemoryEvent>>(
                new DataOutputPath(
                    PerfettoPluginConstants.ProcessMemoryEventCookerPath,
                    nameof(PerfettoProcessMemoryEventCooker.ProcessMemoryEvents)));
            Assert.IsTrue(processMemoryEventData.Count == 10811);
            Assert.IsTrue(processMemoryEventData[0].RssFile == 2822144);
            Assert.IsTrue(processMemoryEventData[1].ProcessName == "/system/bin/init 1");
            UnitTest.TestTableBuild(RuntimeExecutionResults, PerfettoProcessMemoryTable.TableDescriptor, 10811);
        }

        [TestMethod]
        public void TestAndroidGpuTrace()
        {
            LoadTrace(@"..\..\..\..\TestData\Perfetto\androidGpu.pftrace");

            var gpuEvents = RuntimeExecutionResults.QueryOutput<ProcessedEventData<PerfettoGpuCountersEvent>>(
                new DataOutputPath(PerfettoPluginConstants.GpuCountersEventCookerPath, nameof(PerfettoGpuCountersEventCooker.GpuCountersEvents)));

            Assert.IsTrue(gpuEvents.Count == 11564);
            Assert.IsTrue(gpuEvents[0].Value == 0.26908825347823023);
            Assert.IsTrue(gpuEvents[1].StartTimestamp.ToNanoseconds == 1104948);
            UnitTest.TestTableBuild(RuntimeExecutionResults, PerfettoGpuCountersTable.TableDescriptor, 11564);
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
            UnitTest.TestTableBuild(RuntimeExecutionResults, PerfettoGenericEventTable.TableDescriptor, 147906);

            var logcatEventData = RuntimeExecutionResults.QueryOutput<ProcessedEventData<PerfettoLogcatEvent>>(
                new DataOutputPath(
                    PerfettoPluginConstants.LogcatEventCookerPath,
                    nameof(PerfettoLogcatEventCooker.LogcatEvents)));
            Assert.IsTrue(logcatEventData.Count == 43);
            Assert.IsTrue(logcatEventData[0].Message == "type: 97 score: 0.8\n");
            Assert.IsTrue(logcatEventData[1].ProcessName == "Browser");
            UnitTest.TestTableBuild(RuntimeExecutionResults, PerfettoLogcatEventTable.TableDescriptor, 43);

            // Processes
            var processEventData = RuntimeExecutionResults.QueryOutput<ProcessedEventData<PerfettoProcessEvent>>(
                new DataOutputPath(
                    PerfettoPluginConstants.ProcessEventCookerPath,
                    nameof(PerfettoProcessEventCooker.ProcessEvents)));

            Assert.IsTrue(processEventData.Count == 15);
            Assert.IsNull(processEventData[14].AndroidAppId);
            Assert.IsNull(processEventData[14].Uid);
            Assert.IsNull(processEventData[14].CmdLine);
            Assert.IsNull(processEventData[14].ParentUpid);
            Assert.IsNull(processEventData[14].ParentProcess);
            Assert.IsTrue(processEventData[14].Name == "Renderer");
            Assert.IsTrue(processEventData[14].Pid == 17456);
            Assert.IsTrue(processEventData[14].Upid == 14);
            Assert.IsTrue(processEventData[14].StartTimestamp == Timestamp.Zero); // NULL should be at trace start
            Assert.IsTrue(processEventData[14].EndTimestamp == new Timestamp(40409516000)); // NULL should be at trace stop
            UnitTest.TestTableBuild(RuntimeExecutionResults, PerfettoProcessTable.TableDescriptor, 15);
        }

        [TestMethod]
        public void TestJankFrameTrace()
        {
            LoadTrace(@"..\..\..\..\TestData\Perfetto\jankFrame.pftrace");

            var frameEvents = RuntimeExecutionResults.QueryOutput<ProcessedEventData<PerfettoFrameEvent>>(
                new DataOutputPath(PerfettoPluginConstants.FrameEventCookerPath, nameof(PerfettoFrameEventCooker.FrameEvents)));

            Assert.IsTrue(frameEvents.Count == 1219);
            Assert.IsTrue(frameEvents[0].ProcessName == "/system/bin/surfaceflinger");
            Assert.IsTrue(frameEvents[1].StartTimestamp.ToNanoseconds == 7299914108);
            Assert.IsTrue(frameEvents[930].FrameType == "Actual");
            Assert.IsTrue(frameEvents[930].Upid == 1);
            Assert.IsTrue(frameEvents[930].JankTag == "Self Jank");
            Assert.IsTrue(frameEvents[930].SurfaceFrameToken == 0);
            Assert.IsTrue(frameEvents[930].GpuComposition == "0");
            Assert.IsTrue(frameEvents[930].PresentType == "Late Present");
            Assert.IsTrue(frameEvents[930].DisplayFrameToken == 5329);
            Assert.IsTrue(frameEvents[930].PredictionType == "Valid Prediction");
            Assert.IsTrue(frameEvents[930].JankType == "SurfaceFlinger CPU Deadline Missed");
            Assert.IsTrue(frameEvents[930].OnTimeFinish == "0");
            Assert.IsTrue(frameEvents[930].Duration.ToNanoseconds == 28924900);
            UnitTest.TestTableBuild(RuntimeExecutionResults, PerfettoFrameTable.TableDescriptor, 1219);
        }
    }
}
