// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Microsoft.Performance.SDK.Extensibility;
using Microsoft.Performance.SDK.Processing;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Diagnostics.CodeAnalysis;
using PerfettoCds.Pipeline.DataOutput;
using Microsoft.Performance.SDK;
using PerfettoCds.Pipeline.CompositeDataCookers;

namespace PerfettoCds.Pipeline.Tables
{
    [Table]
    public class PerfettoLogcatEventTable
    {
        // Set some sort of max to prevent ridiculous field counts
        public const int AbsoluteMaxFields = 20;

        public static TableDescriptor TableDescriptor => new TableDescriptor(
            Guid.Parse("{1b25fe8d-887c-4de9-850f-284eb4c28ad7}"),
            "Android Logcat Events",
            "All logcat events/messages in the Perfetto trace",
            "Perfetto - Android",
            requiredDataCookers: new List<DataCookerPath> { PerfettoPluginConstants.LogcatEventCookerPath }
        );

        private static readonly ColumnConfiguration StartTimestampColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{01649d8a-6d7b-4024-a07b-b5c1adb6e358}"), "StartTimestamp", "Start timestamp of the event"),
            new UIHints { Width = 180 });

        private static readonly ColumnConfiguration ProcessNameColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{c149eeb0-8f9d-41b1-9513-728bea20535d}"), "ProcessName", "Name of the process that logged the event"),
            new UIHints { Width = 210 });
        
        private static readonly ColumnConfiguration ThreadNameColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{8b0c4b2a-675e-40d2-9c34-164f6a4751f7}"), "ThreadName", "Name of the thread that logged the event"),
            new UIHints { Width = 210 });

        private static readonly ColumnConfiguration PriorityColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{b03c591b-da3d-4866-a762-c44c5017de31}"), "Priority", "Priority of the logcat message"),
            new UIHints { Width = 150 });

        private static readonly ColumnConfiguration TagColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{365a2cd8-1b17-4c52-a23a-35ad8e0b126a}"), "Tag", "Logcat message tag"),
            new UIHints { Width = 120 });

        private static readonly ColumnConfiguration MessageColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{28a51601-ccdc-4a9a-b484-1e85dad75ea5}"), "Message", "Logcat message"),
            new UIHints { Width = 300 });


        public static void BuildTable(ITableBuilder tableBuilder, IDataExtensionRetrieval tableData)
        {
            // Get data from the cooker
            var events = tableData.QueryOutput<ProcessedEventData<PerfettoLogcatEvent>>(
                new DataOutputPath(PerfettoPluginConstants.LogcatEventCookerPath, nameof(PerfettoLogcatEventCooker.LogcatEvents)));

            // Start construction of the column order. Pivot on process and thread
            List<ColumnConfiguration> allColumns = new List<ColumnConfiguration>() 
            {
                ProcessNameColumn,
                ThreadNameColumn,
                TagColumn,
                MessageColumn,
                PriorityColumn,
                TableConfiguration.GraphColumn, // Columns after this get graphed
                StartTimestampColumn
            };

            var tableGenerator = tableBuilder.SetRowCount((int)events.Count);
            var baseProjection = Projection.Index(events);

            tableGenerator.AddColumn(StartTimestampColumn, baseProjection.Compose(x => x.StartTimestamp));
            tableGenerator.AddColumn(ProcessNameColumn, baseProjection.Compose(x => x.ProcessName));
            tableGenerator.AddColumn(ThreadNameColumn, baseProjection.Compose(x => x.ThreadName));
            tableGenerator.AddColumn(PriorityColumn, baseProjection.Compose(x => x.Priority));
            tableGenerator.AddColumn(TagColumn, baseProjection.Compose(x => x.Tag));
            tableGenerator.AddColumn(MessageColumn, baseProjection.Compose(x => x.Message));

            var tableConfig = new TableConfiguration("Perfetto Logcat Events")
            {
                Columns = allColumns,
                Layout = TableLayoutStyle.GraphAndTable
            };
            tableConfig.AddColumnRole(ColumnRole.StartTime, StartTimestampColumn.Metadata.Guid);

            tableBuilder.AddTableConfiguration(tableConfig).SetDefaultTableConfiguration(tableConfig);
        }
    }
}
