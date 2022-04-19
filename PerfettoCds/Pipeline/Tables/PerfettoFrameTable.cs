// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Performance.SDK;
using Microsoft.Performance.SDK.Extensibility;
using Microsoft.Performance.SDK.Processing;
using PerfettoCds.Pipeline.CompositeDataCookers;
using PerfettoCds.Pipeline.DataOutput;

namespace PerfettoCds.Pipeline.Tables
{
    [Table]
    public class PerfettoFrameTable
    {
        public static TableDescriptor TableDescriptor => new TableDescriptor(
            Guid.Parse("{537786ea-9201-4a5b-b696-1e75b715d045}"),
            "Display Frame Events",
            "Displays Expected/Actual Frame events from the Surface Flinger",
            "Perfetto - System",
            defaultLayout: TableLayoutStyle.GraphAndTable,
            requiredDataCookers: new List<DataCookerPath> { PerfettoPluginConstants.FrameEventCookerPath }
        );

        private static readonly ColumnConfiguration ProcessNameColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{66012064-58f5-43a3-ba39-f7ed5a93324e}"), "Process", "Name of the process"),
            new UIHints { Width = 210 });

        private static readonly ColumnConfiguration ProcessIdColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{60e16cc1-8998-4cc2-94ff-a41219627175}"), "Pid", "Process Id"),
            new UIHints { Width = 70 });

        private static readonly ColumnConfiguration FrameTypeColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{09cca658-134a-4f9d-9b15-bfbb298c43e2}"), "FrameType", "Frames can be Expected or Actual frames"),
            new UIHints { Width = 120 });

        private static readonly ColumnConfiguration JankTypeColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{9194517e-94ba-471f-8472-a505762b28e0}"), "JankType", "The kind of jank experienced if any"),
            new UIHints { Width = 210 });

        private static readonly ColumnConfiguration OnTimeFinishColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{2954eb20-961a-40e4-957f-f455e0bede1c}"), "OnTimeFinish", "Whether the app finished the frame on time"),
            new UIHints { Width = 70 });

        private static readonly ColumnConfiguration StartTimestampColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{b20c49ed-e439-4cc2-b0c8-17468e2ef4b8}"), "StartTimestamp", "Start timestamp for the event"),
            new UIHints { Width = 120 });

        private static readonly ColumnConfiguration EndTimestampColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{a3c5f782-48bc-4cbd-94e0-bfd7604e4977}"), "EndTimestamp", "End timestamp for the event"),
            new UIHints { Width = 120 });

        private static readonly ColumnConfiguration DurationColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{49f0442f-bac7-4385-893e-ad343ed2b82a}"), "Duration", "Duration of the event"),
            new UIHints { Width = 70 , AggregationMode = AggregationMode.Max , SortOrder = SortOrder.Descending });

        private static readonly ColumnConfiguration DisplayTokenColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{104a249c-3254-4700-852f-482e9de23feb}"), "DisplayToken", "Display Token"),
            new UIHints { Width = 70 });

        private static readonly ColumnConfiguration SurfaceTokenColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{696ee578-307f-4fe3-a47f-a5088d1de33c}"), "SurfaceToken", "Surface Token"),
            new UIHints { Width = 70 });

        private static readonly ColumnConfiguration PresentTypeColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{ced23290-3c59-403b-9571-51e8cb80e2c3}"), "Present Type", "Whether the frame was presented on time "),
            new UIHints { Width = 100 });

        private static readonly ColumnConfiguration PredictionTypeColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{39c86a0f-d7ec-4a06-8f38-7af88f62adfe}"), "Prediction Type", "Result of frame prediction"),
            new UIHints { Width = 120 });

        private static readonly ColumnConfiguration GpuCompositionColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{596b9907-4ba3-4801-8948-d7075ef75511}"), "GpuComposition", "Whether the frame was GPU composited"),
            new UIHints { Width = 100 });

        private static readonly ColumnConfiguration JankTagColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{96602fe5-4a2e-4222-986f-baf9aed47ce1}"), "JankTag", "Jank tag"),
            new UIHints { Width = 100 });

        public static bool IsDataAvailable(IDataExtensionRetrieval tableData)
        {
            return tableData.QueryOutput<ProcessedEventData<PerfettoFrameEvent>>(
                new DataOutputPath(PerfettoPluginConstants.FrameEventCookerPath, nameof(PerfettoFrameEventCooker.FrameEvents))).Any();
        }

        public static void BuildTable(ITableBuilder tableBuilder, IDataExtensionRetrieval tableData)
        {
            // Get data from the cooker
            var events = tableData.QueryOutput<ProcessedEventData<PerfettoFrameEvent>>(
                new DataOutputPath(PerfettoPluginConstants.FrameEventCookerPath, nameof(PerfettoFrameEventCooker.FrameEvents)));

            var tableGenerator = tableBuilder.SetRowCount((int)events.Count);
            var baseProjection = Projection.Index(events);

            var startProjection = baseProjection.Compose(x => x.StartTimestamp);
            var endProjection = baseProjection.Compose(x => x.EndTimestamp);

            tableGenerator.AddColumn(ProcessNameColumn, baseProjection.Compose(x => x.ProcessName));
            tableGenerator.AddColumn(ProcessIdColumn, baseProjection.Compose(x => x.Upid));
            tableGenerator.AddColumn(FrameTypeColumn, baseProjection.Compose(x => x.FrameType));
            tableGenerator.AddColumn(JankTypeColumn, baseProjection.Compose(x => x.JankType));
            tableGenerator.AddColumn(OnTimeFinishColumn, baseProjection.Compose(x => x.OnTimeFinish));
            tableGenerator.AddColumn(DurationColumn, baseProjection.Compose(x => x.Duration));
            tableGenerator.AddColumn(DisplayTokenColumn, baseProjection.Compose(x => x.DisplayFrameToken));
            tableGenerator.AddColumn(SurfaceTokenColumn, baseProjection.Compose(x => x.SurfaceFrameToken));
            tableGenerator.AddColumn(PresentTypeColumn, baseProjection.Compose(x => x.PresentType));
            tableGenerator.AddColumn(PredictionTypeColumn, baseProjection.Compose(x => x.PredictionType));
            tableGenerator.AddColumn(GpuCompositionColumn, baseProjection.Compose(x => x.GpuComposition));
            tableGenerator.AddColumn(JankTagColumn, baseProjection.Compose(x => x.JankTag));
            tableGenerator.AddColumn(StartTimestampColumn, startProjection);
            tableGenerator.AddColumn(EndTimestampColumn, endProjection);


            var jankFramesConfig = new TableConfiguration("Expected/Actual Frames by Process")
            {
                Columns = new[]
                {
                    ProcessNameColumn,
                    DisplayTokenColumn,
                    SurfaceTokenColumn,
                    FrameTypeColumn,
                    TableConfiguration.PivotColumn, // Columns before this get pivotted on
                    PresentTypeColumn,
                    OnTimeFinishColumn,
                    PredictionTypeColumn,
                    JankTypeColumn,
                    JankTagColumn,
                    GpuCompositionColumn,
                    ProcessIdColumn,
                    DurationColumn,
                    TableConfiguration.GraphColumn, // Columns after this get graphed
                    StartTimestampColumn,
                    EndTimestampColumn
                },
            };
            jankFramesConfig.AddColumnRole(ColumnRole.StartTime, StartTimestampColumn.Metadata.Guid);
            jankFramesConfig.AddColumnRole(ColumnRole.EndTime, EndTimestampColumn.Metadata.Guid);
            jankFramesConfig.AddColumnRole(ColumnRole.Duration, DurationColumn.Metadata.Guid);

            tableBuilder
                .AddTableConfiguration(jankFramesConfig)
                .SetDefaultTableConfiguration(jankFramesConfig);
        }

    }
}
