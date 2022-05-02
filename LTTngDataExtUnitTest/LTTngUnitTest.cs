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
using LTTngDataExtensions.Tables;
using Microsoft.Performance.SDK.Extensibility;
using Microsoft.Performance.SDK.Processing;
using Microsoft.Performance.Toolkit.Engine;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UnitTestCommon;

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

                    // Enable tables used by UI
                    //runtime.EnableTable(CpuTable.TableDescriptor);
                    runtime.EnableTable(DiagnosticMessageTable.TableDescriptor);
                    runtime.EnableTable(DiskTable.TableDescriptor);
                    runtime.EnableTable(ExecutionEventTable.TableDescriptor);
                    runtime.EnableTable(FileEventsTable.TableDescriptor);
                    runtime.EnableTable(GenericEventTable.TableDescriptor);
                    runtime.EnableTable(ModuleEventsTable.TableDescriptor);
                    //runtime.EnableTable(ProcessTable.TableDescriptor);
                    runtime.EnableTable(SyscallTable.TableDescriptor);
                    runtime.EnableTable(ThreadTable.TableDescriptor);

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
        public void DiagnosticMessageTableTest()
        {
            ProcessTrace();

            var eventData = RuntimeExecutionResults.QueryOutput<IReadOnlyList<IDiagnosticMessage>>(
                new DataOutputPath(
                    LTTngDmesgDataCookerPath,
                    nameof(LTTngDmesgDataCooker.DiagnosticMessages)));

            Assert.IsTrue(eventData.Count == 0); // TODO - UT - Trace has no DiagMessages

            UnitTest.TestTableBuild(RuntimeExecutionResults, DiagnosticMessageTable.TableDescriptor, 0, true);
        }

        [TestMethod]
        public void DiskTableTest()
        {
            ProcessTrace();

            var eventData = RuntimeExecutionResults.QueryOutput<IReadOnlyList<DiskActivity>>(
                new DataOutputPath(
                    LTTngDiskDataCookerPath,
                    nameof(LTTngDiskDataCooker.DiskActivity)));

            Assert.IsTrue(eventData.Count == 6437);

            UnitTest.TestTableBuild(RuntimeExecutionResults, DiskTable.TableDescriptor, 6437);
        }

        [TestMethod]
        public void ExecutionEventTableTest()
        {
            ProcessTrace();

            var eventData = RuntimeExecutionResults.QueryOutput<IReadOnlyList<IExecutionEvent>>(
                new DataOutputPath(
                    LTTngThreadDataCookerPath,
                    nameof(LTTngThreadDataCooker.ExecutionEvents)));

            Assert.IsTrue(eventData.Count == 18780);
            UnitTest.TestTableBuild(RuntimeExecutionResults, ExecutionEventTable.TableDescriptor, 18780);
        }

        [TestMethod]
        public void FileEventsTableTest()
        {
            ProcessTrace();

            var eventData = RuntimeExecutionResults.QueryOutput<IReadOnlyList<FileEvent>>(
                new DataOutputPath(
                    LTTngDiskDataCookerPath,
                    nameof(LTTngDiskDataCooker.FileEvents)));

            Assert.IsTrue(eventData.Count == 433143);
            UnitTest.TestTableBuild(RuntimeExecutionResults, FileEventsTable.TableDescriptor, 433143);
        }

        [TestMethod]
        public void GenericEventsTableTest()
        {
            ProcessTrace();

            var eventData = RuntimeExecutionResults.QueryOutput<ProcessedEventData<LTTngGenericEvent>>(
                new DataOutputPath(
                    LTTngGenericEventDataCookerPath,
                    nameof(LTTngGenericEventDataCooker.Events)));

            Assert.IsTrue(eventData.Count == 936356);
            UnitTest.TestTableBuild(RuntimeExecutionResults, GenericEventTable.TableDescriptor, 936356);

            Assert.IsTrue(!String.IsNullOrWhiteSpace(eventData[0].FieldNames[0]));
            Assert.IsTrue(!String.IsNullOrWhiteSpace(eventData[0].FieldValues[0]));
        }

        [TestMethod]
        public void ModuleEventsTableTest()
        {
            ProcessTrace();

            var eventData = RuntimeExecutionResults.QueryOutput<IReadOnlyList<ModuleEvent>>(
                new DataOutputPath(
                    LTTngModuleDataCookerPath,
                    nameof(LTTngModuleDataCooker.ModuleEvents)));

            Assert.IsTrue(eventData.Count == 0);         // TODO - UT - Trace has no ModuleEvents
            UnitTest.TestTableBuild(RuntimeExecutionResults, ModuleEventsTable.TableDescriptor, 0, true);
        }

        [TestMethod]
        public void SyscallTableTest()
        {
            ProcessTrace();

            var eventData = RuntimeExecutionResults.QueryOutput<List<ISyscall>>(
                new DataOutputPath(
                    LTTngSyscallDataCookerPath,
                    nameof(LTTngSyscallDataCooker.Syscalls)));

            Assert.IsTrue(eventData.Count == 441037);
            UnitTest.TestTableBuild(RuntimeExecutionResults, SyscallTable.TableDescriptor, 441037);
        }

        [TestMethod]
        public void ThreadTableTest()
        {
            ProcessTrace();

            var threads = RuntimeExecutionResults.QueryOutput<IReadOnlyList<IThread>>(
                new DataOutputPath(
                    LTTngThreadDataCookerPath,
                    nameof(LTTngThreadDataCooker.Threads)));

            Assert.IsTrue(threads.Count == 82);
            UnitTest.TestTableBuild(RuntimeExecutionResults, ThreadTable.TableDescriptor, 82);
        }
    }
}
