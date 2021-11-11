// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using LTTngCds.CookerData;
using LTTngDataExtensions.SourceDataCookers.Thread;
using Microsoft.Performance.SDK;
using Microsoft.Performance.SDK.Extensibility;
using Microsoft.Performance.SDK.Processing;
using System;
using System.Collections.Generic;

namespace LTTngDataExtensions.Tables
{
    [Table]
    [RequiresSourceCooker(LTTngConstants.SourceId, LTTngThreadDataCooker.Identifier)]
    public class ExecutionEvent
    {
        public static TableDescriptor TableDescriptor = new TableDescriptor(
            Guid.Parse("{91A234C3-3A3C-4230-85DA-76DE1C8E86BA}"),
            "Execution Events",
            "Context Switches History",
            "Linux LTTng",
            defaultLayout: TableLayoutStyle.GraphAndTable);

        private static readonly ColumnConfiguration cpuColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{18AFD6AB-575B-492F-9448-4E21F71DA2C2}"), "CPU"),
                new UIHints { Width = 80, });

        private static readonly ColumnConfiguration nextPidColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{E7DE4BD8-0BE7-4E49-853B-79036971154B}"), "New Process Id"),
                new UIHints { Width = 80, });

        private static readonly ColumnConfiguration nextTidColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{F16E039F-669F-413B-99B3-24410865A420}"), "New Thread Id"),
                new UIHints { Width = 80, });

        private static readonly ColumnConfiguration previousPidColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{B2C1163D-06E2-4610-AC5C-04E0EF819C42}"), "Old Process Id"),
                new UIHints { Width = 80, });

        private static readonly ColumnConfiguration previousTidColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{FDEB823B-D842-4DC6-8613-E4C85782F658}"), "Old Thread Id"),
                new UIHints { Width = 80, });

        private static readonly ColumnConfiguration priorityColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{01FBA4C0-B932-4600-A2C5-C8005358B493}"), "New Priority"),
                new UIHints { Width = 80, });

        private static readonly ColumnConfiguration readyingPidColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{717F75F5-AECE-4500-AB01-5517807B5D00}"), "Readying Process Id"),
                new UIHints { Width = 80, });

        private static readonly ColumnConfiguration readyingTidColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{003B34C9-C5C5-40A3-A65E-CAC68C7B774D}"), "Readying Thread Id"),
                new UIHints { Width = 80, });

        private static readonly ColumnConfiguration previousStateColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{436CCF85-952C-49EB-9DAF-CD8FEC910DDA}"), "New Thread's Previous State"),
                new UIHints { Width = 80, });

        private static readonly ColumnConfiguration nextCommandColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{080EAA64-FE56-4728-8C1D-28D703A0C938}"), "New Command"),
                new UIHints { Width = 80, });

        private static readonly ColumnConfiguration previousCommandColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{EC7E5480-0750-4CFE-92FE-D8724F449B84}"), "Old Command"),
                new UIHints { Width = 80, });

        private static readonly ColumnConfiguration readyTimeColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{2878B695-DD8F-4122-BEE8-3EFB8B1F4C97}"), "Ready"),
                new UIHints { Width = 80, CellFormat = "ms" });

        private static readonly ColumnConfiguration waitTimeColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{DBC4FA4F-8006-4AC4-81C2-13A7785E97B0}"), "Wait"),
                new UIHints { Width = 80, CellFormat = "ms" });

        private static readonly ColumnConfiguration switchInTimeColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{6F301D6D-A06D-483E-BC1A-9756231E8D51}"), "Switch-In Time"),
                new UIHints { Width = 80, });

        private static readonly ColumnConfiguration switchOutTimeColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{324AB05F-5CE4-44CE-BE23-DBDBDF2A62C1}"), "Next Switch-Out Time"),
                new UIHints { Width = 80, });

        private static readonly ColumnConfiguration switchedInTimeColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{5538A25A-1170-44A8-88C3-47235BDE00F2}"), "New Switched-In Time"),
                new UIHints { Width = 80, });

        private static readonly ColumnConfiguration nextThreadPreviousSwitchOutTimeColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{5AB96EE5-FE5C-45AE-9BA2-965EFF4833D8}"), "Last Switch-Out Time"),
                new UIHints { Width = 80, });

        private static readonly ColumnConfiguration cpuUsageInViewportPreset = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{0cf6ffa6-2f41-4460-a201-718c37cbf413}"), "CPU Usage (in view)"),
            new UIHints
            {
                IsVisible = true,
                Width = 100,
                TextAlignment = TextAlignment.Right,
                CellFormat = TimestampFormatter.FormatMillisecondsGrouped,
            });

        private static readonly ColumnConfiguration cpuUsagePreset = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{b668a086-1ed7-446e-bdbb-46adf716c444}"), "CPU Usage"),
            new UIHints
            {
                IsVisible = false,
                Width = 100,
                TextAlignment = TextAlignment.Right,
                CellFormat = TimestampFormatter.FormatMillisecondsGrouped,
            });

        private static readonly ColumnConfiguration percentCpuUsagePreset = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{4c1cc795-d5c2-4a74-8897-d0bf6c98e7ea}"), "% CPU Usage") { IsPercent = true },
            new UIHints
            {
                IsVisible = true,
                Width = 100,
                TextAlignment = TextAlignment.Right,
                CellFormat = ColumnFormats.PercentFormat,
                AggregationMode = AggregationMode.Sum,
                SortOrder = SortOrder.Descending,
                SortPriority = 0,
            });

        public static void BuildTable(ITableBuilder tableBuilder, IDataExtensionRetrieval tableData)
        {
            var threads = tableData.QueryOutput<IReadOnlyList<IExecutionEvent>>(
                DataOutputPath.ForSource(LTTngConstants.SourceId, LTTngThreadDataCooker.Identifier, nameof(LTTngThreadDataCooker.ExecutionEvents)));
            if (threads.Count == 0)
            {
                return;
            }

            const string filterIdleSamplesQuery = "[New Thread Id]:=\"0\"";
            var timelineByCPUTableConfig = new TableConfiguration("Timeline by CPU")
            {
                Columns = new[]
                {
                    cpuColumn,
                    nextPidColumn,
                    nextTidColumn,
                    TableConfiguration.PivotColumn,
                    nextCommandColumn,
                    previousStateColumn,
                    nextThreadPreviousSwitchOutTimeColumn,
                    previousTidColumn,
                    readyingPidColumn,
                    readyingTidColumn,
                    readyTimeColumn,
                    waitTimeColumn,
                    switchedInTimeColumn,
                    priorityColumn,
                    TableConfiguration.GraphColumn,
                    switchInTimeColumn,
                    switchOutTimeColumn
                },
                InitialFilterShouldKeep = false,
                InitialFilterQuery = filterIdleSamplesQuery,
            };

            //timelineByCPUTableConfig.AddColumnRole(ColumnRole.EndThreadId, nextTidColumn);
            timelineByCPUTableConfig.AddColumnRole(ColumnRole.StartTime, switchInTimeColumn);
            timelineByCPUTableConfig.AddColumnRole(ColumnRole.ResourceId, cpuColumn);
            //timelineByCPUTableConfig.AddColumnRole(ColumnRole.WaitEndTime, switchInTimeColumn);
            timelineByCPUTableConfig.AddColumnRole(ColumnRole.Duration, switchedInTimeColumn);

            var utilByProcessCmdTable = new TableConfiguration("Utilization by Process Id, Thread Id, Cmd")
            {
                Columns = new[]
                {
                    nextPidColumn,
                    nextTidColumn,
                    nextCommandColumn,
                    TableConfiguration.PivotColumn,
                    cpuColumn,
                    previousStateColumn,
                    nextThreadPreviousSwitchOutTimeColumn,
                    previousTidColumn,
                    readyingPidColumn,
                    readyingTidColumn,
                    switchInTimeColumn,
                    readyTimeColumn,
                    waitTimeColumn,
                    switchedInTimeColumn,
                    priorityColumn,
                    TableConfiguration.GraphColumn,
                    percentCpuUsagePreset,
                },
                InitialFilterShouldKeep = false,
                InitialFilterQuery = filterIdleSamplesQuery,
            };

            //utilByProcessCmdTable.AddColumnRole(ColumnRole.EndThreadId, nextTidColumn);
            utilByProcessCmdTable.AddColumnRole(ColumnRole.StartTime, switchInTimeColumn);
            utilByProcessCmdTable.AddColumnRole(ColumnRole.ResourceId, cpuColumn);
            //utilByProcessCmdTable.AddColumnRole(ColumnRole.WaitEndTime, switchInTimeColumn);
            utilByProcessCmdTable.AddColumnRole(ColumnRole.Duration, switchedInTimeColumn);

            var utilByCpuTable = new TableConfiguration("Utilization by CPU")
            {
                Columns = new[]
                {
                    cpuColumn,
                    TableConfiguration.PivotColumn,
                    nextPidColumn,
                    nextTidColumn,
                    nextCommandColumn,
                    previousStateColumn,
                    nextThreadPreviousSwitchOutTimeColumn,
                    previousTidColumn,
                    readyingPidColumn,
                    readyingTidColumn,
                    switchInTimeColumn,
                    readyTimeColumn,
                    waitTimeColumn,
                    switchedInTimeColumn,
                    priorityColumn,
                    TableConfiguration.GraphColumn,
                    percentCpuUsagePreset,
                },
                InitialFilterShouldKeep = false,
                InitialFilterQuery = filterIdleSamplesQuery,
            };

            //utilByCpuTable.AddColumnRole(ColumnRole.EndThreadId, nextTidColumn);
            utilByCpuTable.AddColumnRole(ColumnRole.StartTime, switchInTimeColumn);
            utilByCpuTable.AddColumnRole(ColumnRole.ResourceId, cpuColumn);
            //utilByCpuTable.AddColumnRole(ColumnRole.WaitEndTime, switchInTimeColumn);
            utilByCpuTable.AddColumnRole(ColumnRole.Duration, switchedInTimeColumn);

            var table = tableBuilder.AddTableConfiguration(timelineByCPUTableConfig)
                                    .AddTableConfiguration(utilByProcessCmdTable)
                                    .AddTableConfiguration(utilByCpuTable)
                                    .SetDefaultTableConfiguration(utilByProcessCmdTable)
                                    .SetRowCount(threads.Count);

            var switchInTime = Projection.CreateUsingFuncAdaptor((i) => threads[i].SwitchInTime);
            var switchOutTime = Projection.CreateUsingFuncAdaptor((i) => threads[i].SwitchOutTime);

            table.AddColumn(cpuColumn, Projection.CreateUsingFuncAdaptor((i) => threads[i].Cpu));
            table.AddColumn(nextPidColumn, Projection.CreateUsingFuncAdaptor((i) => threads[i].NextPid));
            table.AddColumn(nextTidColumn, Projection.CreateUsingFuncAdaptor((i) => threads[i].NextTid));
            table.AddColumn(previousStateColumn, Projection.CreateUsingFuncAdaptor((i) => threads[i].PreviousState));
            table.AddColumn(nextThreadPreviousSwitchOutTimeColumn, Projection.CreateUsingFuncAdaptor((i) => threads[i].NextThreadPreviousSwitchOutTime));
            table.AddColumn(previousTidColumn, Projection.CreateUsingFuncAdaptor((i) => threads[i].PreviousTid));
            table.AddColumn(readyingPidColumn, Projection.CreateUsingFuncAdaptor((i) => threads[i].ReadyingPid));
            table.AddColumn(readyingTidColumn, Projection.CreateUsingFuncAdaptor((i) => threads[i].ReadyingTid));
            table.AddColumn(readyTimeColumn, Projection.CreateUsingFuncAdaptor((i) => threads[i].ReadyTime));
            table.AddColumn(waitTimeColumn, Projection.CreateUsingFuncAdaptor((i) => threads[i].WaitTime));
            table.AddColumn(switchedInTimeColumn, Projection.CreateUsingFuncAdaptor((i) => threads[i].SwitchOutTime - threads[i].SwitchInTime));
            table.AddColumn(priorityColumn, Projection.CreateUsingFuncAdaptor((i) => threads[i].Priority));
            table.AddColumn(switchInTimeColumn, switchInTime);
            table.AddColumn(switchOutTimeColumn, switchOutTime);
            table.AddColumn(previousPidColumn, Projection.CreateUsingFuncAdaptor((i) => threads[i].PreviousPid));
            table.AddColumn(nextCommandColumn, Projection.CreateUsingFuncAdaptor((i) => threads[i].NextImage));
            table.AddColumn(previousCommandColumn, Projection.CreateUsingFuncAdaptor((i) => threads[i].PreviousImage));


            // Time the thread switching in switches out
            var viewportClippedSwitchOutTimeForNextOnCpuColumn = Projection.ClipTimeToVisibleDomain.Create(switchOutTime);

            // Switch in time is the thread switching in, which is the switch out time of the thread switching out on the CPU
            var viewportClippedSwitchOutTimeForPreviousOnCpuColumn = Projection.ClipTimeToVisibleDomain.Create(switchInTime);

            IProjection<int, TimestampDelta> cpuUsageInViewportColumn = Projection.Select(
                    viewportClippedSwitchOutTimeForNextOnCpuColumn,
                    viewportClippedSwitchOutTimeForPreviousOnCpuColumn,
                    new ReduceTimeSinceLastDiff());

            var percentCpuUsageColumn = Projection.VisibleDomainRelativePercent.Create(cpuUsageInViewportColumn);

            var cpuUsageColumn = Projection.Select(switchOutTime, switchInTime, new ReduceTimeSinceLastDiff());

            table.AddColumn(cpuUsageInViewportPreset, cpuUsageInViewportColumn);
            table.AddColumn(cpuUsagePreset, cpuUsageColumn);
            table.AddColumn(percentCpuUsagePreset, percentCpuUsageColumn);
        }
    }

    struct ReduceTimeSinceLastDiff
        : IFunc<int, Timestamp, Timestamp, TimestampDelta>
    {
        public TimestampDelta Invoke(int value, Timestamp timeSinceLast1, Timestamp timeSinceLast2)
        {
            return timeSinceLast1 - timeSinceLast2;
        }
    }

}
