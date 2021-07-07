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

        private static DataCookerPath PerfettoGenericEventDataCookerPath;
        private static DataCookerPath PerfettoSliceCookerPath;
        private static DataCookerPath PerfettoArgCookerPath;
        private static DataCookerPath PerfettoThreadTrackCookerPath;
        private static DataCookerPath PerfettoThreadCookerPath;
        private static DataCookerPath PerfettoProcessCookerPath;


        public static void ProcessTrace()
        {
            lock (IsTraceProcessedLock)
            {
                if (!IsTraceProcessed)
                {
                    // Input data
                    string perfettoTrace = @"..\..\..\..\TestData\Perfetto\sample.perfetto-trace";
                    var perfettoDataPath = new FileInfo(perfettoTrace);
                    Assert.IsTrue(perfettoDataPath.Exists);

                    // Approach #1 - Engine - Doesn't test tables UI but tests processing
                    var runtime = Engine.Create();

                    runtime.AddFile(perfettoDataPath.FullName);

                    // Enable our various types of data cookers
                    var perfettoSliceCooker = new PerfettoSliceCooker();
                    PerfettoSliceCookerPath = perfettoSliceCooker.Path;
                    runtime.EnableCooker(PerfettoSliceCookerPath);

                    var perfettoArgCooker = new PerfettoArgCooker();
                    PerfettoArgCookerPath = perfettoArgCooker.Path;
                    runtime.EnableCooker(PerfettoArgCookerPath);

                    var perfettoThreadTrackCooker = new PerfettoThreadTrackCooker();
                    PerfettoThreadTrackCookerPath = perfettoThreadTrackCooker.Path;
                    runtime.EnableCooker(PerfettoThreadTrackCookerPath);

                    var perfettoThreadCooker = new PerfettoThreadCooker();
                    PerfettoThreadCookerPath = perfettoThreadCooker.Path;
                    runtime.EnableCooker(PerfettoThreadCookerPath);

                    var perfettoProcessCooker = new PerfettoProcessCooker();
                    PerfettoProcessCookerPath = perfettoProcessCooker.Path;
                    runtime.EnableCooker(PerfettoProcessCookerPath);

                    var perfettoGenericEventDataCooker = new PerfettoGenericEventCooker();
                    PerfettoGenericEventDataCookerPath = perfettoGenericEventDataCooker.Path;
                    runtime.EnableCooker(PerfettoGenericEventDataCookerPath);

                    //
                    // Process our data.
                    //
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
            ProcessTrace();

            var eventData = RuntimeExecutionResults.QueryOutput<ProcessedEventData<PerfettoGenericEvent>>(
                new DataOutputPath(
                    PerfettoGenericEventDataCookerPath,
                    nameof(PerfettoGenericEventCooker.GenericEvents)));

            Assert.IsTrue(eventData.Count == 3);

            // Ensures join with slices table worked
            Assert.IsTrue(eventData[0].EventName == "name1");

            // Ensures joins with thread_track, thread, and process table worked
            Assert.IsTrue(eventData[1].Thread == "t1 1");
            Assert.IsTrue(eventData[2].Process == " 5");

            // Ensures join with args table worked
            Assert.IsTrue(eventData[0].ArgKeys.Count == 1);
            Assert.IsTrue(eventData[0].ArgKeys[0] == "chrome_user_event.action");
        }
    }
}
