// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using LTTngDataExtensions.DataOutputTypes;
using LTTngDataExtensions.SourceDataCookers;
using LTTngDataExtensions.SourceDataCookers.Diagnostic_Messages;
using LTTngDataExtensions.SourceDataCookers.Disk;
using LTTngDataExtensions.SourceDataCookers.Module;
using LTTngDataExtensions.SourceDataCookers.Syscall;
using LTTngDataExtensions.SourceDataCookers.Thread;
using Microsoft.Performance.SDK.Extensibility;
using Microsoft.Performance.SDK.Processing;
using Microsoft.Performance.Toolkit.Engine;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LTTngDataExtUnitTest
{
    [TestClass]
    public class LTTngUnitTest
    {
        public static bool IsTraceProcessed = false;
        public static object IsTraceProcessedLock = new object();

        private static RuntimeExecutionResults RuntimeExecutionResults;

        private static DataCookerPath LTTngGenericEventDataCookerPath;
        private static DataCookerPath LTTngSyscallDataCookerPath;
        private static DataCookerPath LTTngThreadDataCookerPath;
        private static DataCookerPath LTTngDmesgDataCookerPath;
        private static DataCookerPath LTTngModuleDataCookerPath;
        private static DataCookerPath LTTngDiskDataCookerPath;

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

                    // Approach #1 - Engine - Doesn't test tables UI but tests processing
                    var runtime = Engine.Create(new FileDataSource(lttngDataPath.FullName));

                    // Enable our various types of data
                    var lttngGenericEventDataCooker = new LTTngGenericEventDataCooker();
                    LTTngGenericEventDataCookerPath = lttngGenericEventDataCooker.Path;
                    runtime.EnableCooker(LTTngGenericEventDataCookerPath);

                    var lttngSyscallDataCooker = new LTTngSyscallDataCooker();
                    LTTngSyscallDataCookerPath = lttngSyscallDataCooker.Path;
                    runtime.EnableCooker(LTTngSyscallDataCookerPath);

                    var lttngThreadDataCooker = new LTTngThreadDataCooker();
                    LTTngThreadDataCookerPath = lttngThreadDataCooker.Path;
                    runtime.EnableCooker(LTTngThreadDataCookerPath);

                    var lttngDmesgDataCooker = new LTTngDmesgDataCooker();
                    LTTngDmesgDataCookerPath = lttngDmesgDataCooker.Path;
                    runtime.EnableCooker(LTTngDmesgDataCookerPath);

                    var lttngModuleDataCooker = new LTTngModuleDataCooker();
                    LTTngModuleDataCookerPath = lttngModuleDataCooker.Path;
                    runtime.EnableCooker(LTTngModuleDataCookerPath);

                    var lttngDiskDataCooker = new LTTngDiskDataCooker();
                    LTTngDiskDataCookerPath = lttngDiskDataCooker.Path;
                    runtime.EnableCooker(LTTngDiskDataCookerPath);

                    //
                    // Process our data.
                    //

                    RuntimeExecutionResults = runtime.Process();

                    IsTraceProcessed = true;
                }
            }
        }

        [TestMethod]
        public void ProcessTraceAsFolder()
        {
            // Input data
            string[] lttngData = { @"..\..\..\..\TestData\LTTng\lttng-kernel-trace.ctf" };

            string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

            using (var zipFile = ZipFile.OpenRead(lttngData[0]))
            {
                zipFile.ExtractToDirectory(tempDirectory);
            }

            using (var dataSourceSet = DataSourceSet.Create())
            {
                var ds = new DirectoryDataSource(tempDirectory);
                dataSourceSet.AddDataSource(ds);

                // Approach #1 - Engine - Doesn't test tables UI but tests processing
                using (var runtime = Engine.Create(new EngineCreateInfo(dataSourceSet.AsReadOnly())))
                {
                    //
                    // We do not assert that any cookers are enabled since we did not explicitly enable cookers here
                    //

                    Assert.IsTrue(ds.IsDirectory());
                    Assert.IsTrue(runtime.AvailableTables.Count() >= 1);
                }
            }


            Directory.Delete(tempDirectory, true);
        }

        [TestMethod]
        public void DiagnosticMessageTable()
        {
            ProcessTrace();

            var eventData = RuntimeExecutionResults.QueryOutput<IReadOnlyList<IDiagnosticMessage>>(
                new DataOutputPath(
                    LTTngDmesgDataCookerPath,
                    nameof(LTTngDmesgDataCooker.DiagnosticMessages)));

            Assert.IsTrue(eventData.Count == 0); // TODO - UT - Trace has no DiagMessages
        }

        [TestMethod]
        public void DiskTable()
        {
            ProcessTrace();

            var eventData = RuntimeExecutionResults.QueryOutput<IReadOnlyList<DiskActivity>>(
                new DataOutputPath(
                    LTTngDiskDataCookerPath,
                    nameof(LTTngDiskDataCooker.DiskActivity)));

            Assert.IsTrue(eventData.Count > 0);
        }

        [TestMethod]
        public void ExecutionEventTable()
        {
            ProcessTrace();

            var eventData = RuntimeExecutionResults.QueryOutput<IReadOnlyList<IExecutionEvent>>(
                new DataOutputPath(
                    LTTngThreadDataCookerPath,
                    nameof(LTTngThreadDataCooker.ExecutionEvents)));

            Assert.IsTrue(eventData.Count > 0);
        }

        [TestMethod]
        public void FileEventsTable()
        {
            ProcessTrace();

            var eventData = RuntimeExecutionResults.QueryOutput<IReadOnlyList<FileEvent>>(
                new DataOutputPath(
                    LTTngDiskDataCookerPath,
                    nameof(LTTngDiskDataCooker.FileEvents)));

            Assert.IsTrue(eventData.Count > 0);
        }

        [TestMethod]
        public void GenericEventsTable()
        {
            ProcessTrace();

            var eventData = RuntimeExecutionResults.QueryOutput<ProcessedEventData<LTTngGenericEvent>>(
                new DataOutputPath(
                    LTTngGenericEventDataCookerPath,
                    nameof(LTTngGenericEventDataCooker.Events)));

            Assert.IsTrue(eventData.Count > 0);

            Assert.IsTrue(!String.IsNullOrWhiteSpace(eventData[0].FieldNames[0]));
            Assert.IsTrue(!String.IsNullOrWhiteSpace(eventData[0].FieldValues[0]));
        }

        [TestMethod]
        public void ModuleEventsTable()
        {
            ProcessTrace();

            var eventData = RuntimeExecutionResults.QueryOutput<IReadOnlyList<ModuleEvent>>(
                new DataOutputPath(
                    LTTngModuleDataCookerPath,
                    nameof(LTTngModuleDataCooker.ModuleEvents)));

            Assert.IsTrue(eventData.Count == 0);         // TODO - UT - Trace has no ModuleEvents
        }

        [TestMethod]
        public void SyscallTable()
        {
            ProcessTrace();

            var eventData = RuntimeExecutionResults.QueryOutput<List<ISyscall>>(
                new DataOutputPath(
                    LTTngSyscallDataCookerPath,
                    nameof(LTTngSyscallDataCooker.Syscalls)));

            Assert.IsTrue(eventData.Count > 0);
        }

        [TestMethod]
        public void ThreadTable()
        {
            ProcessTrace();

            var threads = RuntimeExecutionResults.QueryOutput<IReadOnlyList<IThread>>(
                new DataOutputPath(
                    LTTngThreadDataCookerPath,
                    nameof(LTTngThreadDataCooker.Threads)));

            Assert.IsTrue(threads.Count > 0);
        }
    }
}
