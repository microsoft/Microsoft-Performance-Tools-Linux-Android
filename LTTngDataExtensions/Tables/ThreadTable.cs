// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using LTTngDataExtensions.SourceDataCookers.Thread;
using System;
using System.Collections.Generic;
using Microsoft.Performance.SDK.Extensibility;
using Microsoft.Performance.SDK.Processing;
using LTTngCds.CookerData;

namespace LTTngDataExtensions.Tables
{
    [Table]
    [RequiresSourceCooker(LTTngConstants.SourceId, LTTngThreadDataCooker.Identifier)]
    public class ThreadTable
    {
        public static TableDescriptor TableDescriptor = new TableDescriptor(
            Guid.Parse("{D42D0B5A-59CD-4294-8BF6-384CA7281984}"),
            "Threads",
            "Threads History",
            "Linux LTTng",
            defaultLayout: TableLayoutStyle.GraphAndTable);

        private static readonly ColumnConfiguration threadIdColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{E5A13736-7C08-48D4-9383-01FDE6474144}"), "Thread Id"),
                new UIHints { Width = 80, });

        private static readonly ColumnConfiguration processIdColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{30CAC3A3-1ADB-416E-8F83-18D17B2B10DB}"), "Process Id"),
                new UIHints { Width = 80, });

        private static readonly ColumnConfiguration commandColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{70B3FF3A-034C-46C4-B176-2153022D0E9C}"), "Command"),
                new UIHints { Width = 80, });

        private static readonly ColumnConfiguration threadStartTimeColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{AF8CBBED-5D44-4A11-9FB9-FC1AAFAE7813}"), "Start Time"),
                new UIHints { Width = 80, AggregationMode = AggregationMode.Min });

        private static readonly ColumnConfiguration threadExitTimeColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{E1DE3E9F-0244-486F-9332-17093BDF54A7}"), "Exit Time"),
                new UIHints { Width = 80, AggregationMode = AggregationMode.Max });

        private static readonly ColumnConfiguration threadLifespanColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{D4E91F9D-1AFB-4B60-8E2C-97FDC93DEE95}"), "Lifespan", "Time elapsed betweeen creation and termination of the thread"),
                new UIHints { Width = 80, CellFormat = "ms" });

        private static readonly ColumnConfiguration threadExecTimeColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{F4DE5B92-7579-4167-9C3F-C0EE8ACD1E32}"), "Executing Time", "Time spent executing in a CPU"),
                new UIHints { Width = 80, CellFormat = "ms", AggregationMode = AggregationMode.Sum});

        private static readonly ColumnConfiguration threadReadyTimeColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{E3731646-BD47-4AFC-8DAF-64A07FB7F1CA}"), "Ready Time", "Time spent ready to execute"),
                new UIHints { Width = 80, CellFormat = "ms" });

        private static readonly ColumnConfiguration threadRunningTimeColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{62B861EC-4094-4A4A-A68C-7C31A5E43DF1}"), "Running Time", "Time spent either executing or ready to be executed"),
                new UIHints { Width = 80, CellFormat = "ms" });

        private static readonly ColumnConfiguration threadSleepTimeColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{1F273569-1F87-4FFD-B25C-0A92E6A5C022}"), "Sleeping Time", "Time spent sleeping in an Interruptible state"),
                new UIHints { Width = 80, CellFormat = "ms" });

        private static readonly ColumnConfiguration threadDiskSleepTimeColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{D296F6C7-8F33-478F-9293-2C07AF9EF52D}"), "Disk Sleeping Time", "Time spent sleeping in an Uninterruptible state"),
                new UIHints { Width = 80, CellFormat = "ms" });

        private static readonly ColumnConfiguration threadWaitingTimeColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{8977D324-3CFF-4C9F-A140-12EA7AA40DCA}"), "Waiting Time", "Time spent either in sleeping or disk sleeping state"),
                new UIHints { Width = 80, CellFormat = "ms" });

        private static readonly ColumnConfiguration threadStoppedTimeColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{DCE464FD-9E3F-494F-B8D4-0AD02DE0EB95}"), "Stopped Time", "Time spent stopped"),
                new UIHints { Width = 80, CellFormat = "ms" });

        private static readonly ColumnConfiguration threadParkedTimeColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{F16C1873-08FE-4361-A400-56116BD1985D}"), "Parked Time", "Time spent parked"),
                new UIHints { Width = 80, CellFormat = "ms" });

        private static readonly ColumnConfiguration threadIdleTimeColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{6F29EABD-12B9-47BD-A78A-BD885456437F}"), "Idle time", "Time spent idle"),
                new UIHints { Width = 80, CellFormat = "ms" });

        public static void BuildTable(ITableBuilder tableBuilder, IDataExtensionRetrieval tableData)
        {
            var threads = tableData.QueryOutput<IReadOnlyList<IThread>>(
                DataOutputPath.ForSource(LTTngConstants.SourceId, LTTngThreadDataCooker.Identifier, nameof(LTTngThreadDataCooker.Threads)));
            if (threads.Count == 0)
            {
                return;
            }

            var config = new TableConfiguration("ThreadsByProcessId")
            {
                Columns = new[]
                {
                    processIdColumn,
                    threadIdColumn,
                    commandColumn,
                    TableConfiguration.PivotColumn,
                    threadExecTimeColumn,
                    threadReadyTimeColumn,
                    threadRunningTimeColumn,
                    threadSleepTimeColumn,
                    threadDiskSleepTimeColumn,
                    threadWaitingTimeColumn,
                    threadIdleTimeColumn,
                    TableConfiguration.GraphColumn,
                    threadStartTimeColumn,
                    threadExitTimeColumn
                },
            };

            config.AddColumnRole(ColumnRole.StartTime, threadStartTimeColumn);
            config.AddColumnRole(ColumnRole.EndTime, threadExitTimeColumn);

            var table = tableBuilder.AddTableConfiguration(config)
                                    .SetDefaultTableConfiguration(config)
                                    .SetRowCount(threads.Count);

            table.AddColumn(threadIdColumn, Projection.CreateUsingFuncAdaptor((i) => threads[i].ThreadId));
            table.AddColumn(processIdColumn, Projection.CreateUsingFuncAdaptor((i) => threads[i].ProcessId));
            table.AddColumn(commandColumn, Projection.CreateUsingFuncAdaptor((i) => threads[i].Command));
            table.AddColumn(threadExecTimeColumn, Projection.CreateUsingFuncAdaptor((i) => threads[i].ExecTime));
            table.AddColumn(threadReadyTimeColumn, Projection.CreateUsingFuncAdaptor((i) => threads[i].ReadyTime));
            table.AddColumn(threadRunningTimeColumn, Projection.CreateUsingFuncAdaptor((i) => threads[i].ExecTime + threads[i].ReadyTime));
            table.AddColumn(threadSleepTimeColumn, Projection.CreateUsingFuncAdaptor((i) => threads[i].SleepTime));
            table.AddColumn(threadDiskSleepTimeColumn, Projection.CreateUsingFuncAdaptor((i) => threads[i].DiskSleepTime));
            table.AddColumn(threadWaitingTimeColumn, Projection.CreateUsingFuncAdaptor((i) => threads[i].SleepTime + threads[i].DiskSleepTime));
            table.AddColumn(threadStoppedTimeColumn, Projection.CreateUsingFuncAdaptor((i) => threads[i].StoppedTime));
            table.AddColumn(threadParkedTimeColumn, Projection.CreateUsingFuncAdaptor((i) => threads[i].ParkedTime));
            table.AddColumn(threadIdleTimeColumn, Projection.CreateUsingFuncAdaptor((i) => threads[i].IdleTime));
            table.AddColumn(threadStartTimeColumn, Projection.CreateUsingFuncAdaptor((i) => threads[i].StartTime));
            table.AddColumn(threadExitTimeColumn, Projection.CreateUsingFuncAdaptor((i) => threads[i].ExitTime));
            table.AddColumn(threadLifespanColumn, Projection.CreateUsingFuncAdaptor((i) => threads[i].ExitTime - threads[i].StartTime));
        }
    }
}
