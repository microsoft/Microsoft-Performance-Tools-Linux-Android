// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using LttngDataExtensions.SourceDataCookers.Process;
using Microsoft.Performance.SDK;
using Microsoft.Performance.SDK.Extensibility;
using Microsoft.Performance.SDK.Processing;

namespace LttngDataExtensions.Tables
{
/*
    [Table]
    [RequiresCooker("Lttng/ProcessDataCooker")]
    [PrebuiltConfigurationsFilePath("Resources\\ProcessTablePrebuiltConfigurations.json")]
    public class ProcessTable
    {
        public static TableDescriptor TableDescriptor = new TableDescriptor(
            Guid.Parse("{0185E156-6759-4D1A-AA85-648ECE0EC123}"),
            "Processes",
            "Processes");

        private static readonly ColumnConfiguration processNameColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{A4A52F70-5090-44F8-B043-5F45849AA322}"), "Process Name"),
                new UIHints { Width = 80, });

        private static readonly ColumnConfiguration processPathColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{48FB4171-D473-425B-B7CD-7EBE6DB86547}"), "Process Path"),
                new UIHints { Width = 80, });

        private static readonly ColumnConfiguration processIdColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{22E7EB83-C46E-4F8F-93EE-AE18C3483338}"), "Pid"),
                new UIHints { Width = 80, });

        private static readonly ColumnConfiguration parentProcessIdColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{DE736B58-53B4-49F4-9AC6-2E9766EA1D93}"), "Parent Pid"),
                new UIHints { Width = 80, });

        private static readonly ColumnConfiguration forkTimeColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{75FD3DEB-21A1-4A9A-8909-3BFE28638F4A}"), "Fork Time"),
                new UIHints { Width = 80, });

        private static readonly ColumnConfiguration execTimeColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{278FD637-D59C-4891-9AF9-4618ECB35A6B}"), "Exec Time"),
                new UIHints { Width = 80, });

        private static readonly ColumnConfiguration exitTimeColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{26F1FD74-AADE-40C1-BDED-70EB88D3B09F}"), "Exit Time"),
                new UIHints { Width = 80, });

        private static readonly ColumnConfiguration waitTimeColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{E9214C84-A1C3-430B-833E-5F7E700EF6F0}"), "Wait Time"),
                new UIHints { Width = 80, });

        private static readonly ColumnConfiguration freeTimeColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{1782091F-4E57-4F18-BC18-DC1B8374134B}"), "Free Time"),
                new UIHints { Width = 80, });

        public static void BuildTable(ITableBuilder tableBuilder, IDataExtensionRetrieval tableData)
        {
            var processes = tableData.QueryOutput<IReadOnlyList<IProcess>>(
                DataOutputPath.Create("Lttng/ProcessDataCooker/Processes"));
            if (processes.Count == 0)
            {
                return;
            }

            var table = tableBuilder.SetRowCount(processes.Count);

            table.AddColumn(processNameColumn, Projection.CreateUsingFuncAdaptor((i) => processes[i].Name));
            table.AddColumn(processPathColumn, Projection.CreateUsingFuncAdaptor((i) => processes[i].Path));
            table.AddColumn(processIdColumn, Projection.CreateUsingFuncAdaptor((i) => processes[i].ProcessId));
            table.AddColumn(parentProcessIdColumn, Projection.CreateUsingFuncAdaptor((i) => processes[i].ParentProcessId));
            table.AddColumn(forkTimeColumn, Projection.CreateUsingFuncAdaptor((i) => new Timestamp((long)processes[i].ForkTime)));
            table.AddColumn(execTimeColumn, Projection.CreateUsingFuncAdaptor((i) => new Timestamp((long)processes[i].ExecTime)));
            table.AddColumn(exitTimeColumn, Projection.CreateUsingFuncAdaptor((i) => new Timestamp((long)processes[i].ExitTime)));
            table.AddColumn(waitTimeColumn, Projection.CreateUsingFuncAdaptor((i) => new TimeSpan((long)processes[i].WaitTime)));
            table.AddColumn(freeTimeColumn, Projection.CreateUsingFuncAdaptor((i) => new TimeSpan((long)processes[i].FreeTime)));
        }
    }*/
}