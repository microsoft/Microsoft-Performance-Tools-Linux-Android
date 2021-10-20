// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using LTTngCds.CookerData;
using LTTngDataExtensions.SourceDataCookers.Module;
using Microsoft.Performance.SDK.Extensibility;
using Microsoft.Performance.SDK.Processing;

namespace LTTngDataExtensions.Tables
{
    [Table]
    [RequiresSourceCooker(LTTngConstants.SourceId, LTTngModuleDataCooker.Identifier)]
    public class ModuleEventsTable
    {
        public static TableDescriptor TableDescriptor = new TableDescriptor(
            Guid.Parse("{2CA2F865-5863-47F3-A4A4-6F8FDABE8F9F}"),
            "Module Events",
            "Module Events",
            "Linux LTTng");

        private static readonly ColumnConfiguration eventTypeColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{3568A0A0-6E9B-441C-965E-C15ECDD8A99D}"), "Event Type"),
                new UIHints { Width = 80, });
        private static readonly ColumnConfiguration instructionPointerColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{E0E0A513-5531-40B0-A453-2B73616EBBC6}"), "Instruction Pointer"),
                new UIHints { Width = 80, });

        private static readonly ColumnConfiguration moduleNameColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{81C1917E-8C6F-4011-9AB6-268A539454D8}"), "Module Name"),
                new UIHints { Width = 80, });

        private static readonly ColumnConfiguration refCountColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{345CDB2D-4543-474C-9C63-C252BAF6BE30}"), "Ref Count"),
                new UIHints { Width = 80, });

        private static readonly ColumnConfiguration timestampColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{FD6C27B1-0E05-49D7-82BA-09EBE9006C41}"), "Timestamp"),
                new UIHints { Width = 80, CellFormat = "ms" });

        private static readonly ColumnConfiguration threadIdColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{852D5404-079B-4E56-BECA-C631EBE1BAA2}"), "Thread Id"),
                new UIHints { Width = 80, });

        private static readonly ColumnConfiguration processIdColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{CD896889-585E-4104-8D64-D22E0D5A90FF}"), "Process Id"),
                new UIHints { Width = 80, });

        private static readonly ColumnConfiguration commandColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{FE56A50D-5CF2-4BD9-8A87-8DED7F67CAB0}"), "Thread's Command"),
                new UIHints { Width = 80, });

        public static void BuildTable(ITableBuilder tableBuilder, IDataExtensionRetrieval tableData)
        {
            var moduleEvents = tableData.QueryOutput<IReadOnlyList<IModuleEvent>>(
                DataOutputPath.ForSource(LTTngConstants.SourceId, LTTngModuleDataCooker.Identifier, nameof(LTTngModuleDataCooker.ModuleEvents)));
            if (moduleEvents.Count == 0)
            {
                return;
            }

            var defaultConfig = new TableConfiguration("Module Events")
            {
                Columns = new[]
                {
                    moduleNameColumn,
                    eventTypeColumn,
                    TableConfiguration.PivotColumn,
                    instructionPointerColumn,
                    refCountColumn,
                    threadIdColumn,
                    processIdColumn,
                    commandColumn,
                    TableConfiguration.GraphColumn,
                    timestampColumn
                },
                Layout = TableLayoutStyle.GraphAndTable,
            };

            defaultConfig.AddColumnRole(ColumnRole.StartTime, timestampColumn);
            defaultConfig.AddColumnRole(ColumnRole.EndTime, timestampColumn);

            var table = tableBuilder.AddTableConfiguration(defaultConfig)
                                    .SetDefaultTableConfiguration(defaultConfig)
                                    .SetRowCount(moduleEvents.Count);

            table.AddColumn(eventTypeColumn, Projection.CreateUsingFuncAdaptor((i) => moduleEvents[i].EventType));
            table.AddColumn(moduleNameColumn, Projection.CreateUsingFuncAdaptor((i) => moduleEvents[i].ModuleName));
            table.AddColumn(instructionPointerColumn, Projection.CreateUsingFuncAdaptor((i) => moduleEvents[i].InstructionPointer));
            table.AddColumn(refCountColumn, Projection.CreateUsingFuncAdaptor((i) => moduleEvents[i].RefCount));
            table.AddColumn(threadIdColumn, Projection.CreateUsingFuncAdaptor((i) => moduleEvents[i].ThreadId));
            table.AddColumn(processIdColumn, Projection.CreateUsingFuncAdaptor((i) => moduleEvents[i].ProcessId));
            table.AddColumn(commandColumn, Projection.CreateUsingFuncAdaptor((i) => moduleEvents[i].ProcessCommand));
            table.AddColumn(timestampColumn, Projection.CreateUsingFuncAdaptor((i) => moduleEvents[i].Time));
        }
    }
}

