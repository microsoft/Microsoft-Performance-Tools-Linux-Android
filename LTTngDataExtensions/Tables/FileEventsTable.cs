// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using LTTngDataExtensions.DataOutputTypes;
using LTTngDataExtensions.SourceDataCookers.Disk;
using Microsoft.Performance.SDK.Extensibility;
using Microsoft.Performance.SDK;
using Microsoft.Performance.SDK.Processing;
using LTTngCds.CookerData;
using System.Linq;

namespace LTTngDataExtensions.Tables
{
    [Table]
    [RequiresSourceCooker(LTTngConstants.SourceId, LTTngDiskDataCooker.Identifier)]
    public class FileEventsTable
    {
        public static TableDescriptor TableDescriptor = new TableDescriptor(
            Guid.Parse("{A636196C-2FD9-453D-B315-2FC07AD06A26}"),
            "FileEvents",
            "File Events",
            "Linux LTTng",
            defaultLayout: TableLayoutStyle.GraphAndTable);

        private static readonly ColumnConfiguration fileEventNameColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{A3BA2CFE-1390-4375-9054-3C2D993B8219}"), "Event"),
                new UIHints { Width = 80, });

        private static readonly ColumnConfiguration fileEventThreadIdColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{29CC5C7B-73CD-4120-A6F9-3F7F446A20C3}"), "Thread Id"),
                new UIHints { Width = 80, });

        private static readonly ColumnConfiguration fileEventProcessIdColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{7B39A69E-9773-4598-83C1-E6878012FB8D}"), "Process Id"),
                new UIHints { Width = 80, });

        private static readonly ColumnConfiguration fileEventCommandColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{B1CCA7B9-3737-4B9C-A967-65D72667497D}"), "Thread's Command"),
                new UIHints { Width = 80, });

        private static readonly ColumnConfiguration fileEventStartTimeColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{C00F0244-914A-4A03-A4ED-47CF94940DE8}"), "Start Time"),
                new UIHints { Width = 80, });

        private static readonly ColumnConfiguration fileEventEndTimeColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{DB3593DB-C8B2-452A-847A-3F302307B8C4}"), "End Time"),
                new UIHints { Width = 80, });

        private static readonly ColumnConfiguration fileEventDurationColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{B86947FC-86FD-4346-B7CB-D0DB4FF2635D}"), "Duration"),
                new UIHints { Width = 80, CellFormat = "ms" });

        private static readonly ColumnConfiguration fileEventSizeColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{1C55AEC5-4351-4227-A48A-6D8E00A2F2D3}"), "Size"),
                new UIHints { Width = 80, });

        private static readonly ColumnConfiguration fileEventFilePathColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{62A57C1A-45AE-4A81-B2D9-850A2C8D53C8}"), "File"),
                new UIHints { Width = 80, });

        public static bool IsDataAvailable(IDataExtensionRetrieval tableData)
        {
            return tableData.QueryOutput<IReadOnlyList<IFileEvent>>(
                DataOutputPath.ForSource(LTTngConstants.SourceId, LTTngDiskDataCooker.Identifier, nameof(LTTngDiskDataCooker.FileEvents))).Any();
        }

        public static void BuildTable(ITableBuilder tableBuilder, IDataExtensionRetrieval tableData)
        {
            var fileEvents = tableData.QueryOutput<IReadOnlyList<IFileEvent>>(
                DataOutputPath.ForSource(LTTngConstants.SourceId, LTTngDiskDataCooker.Identifier, nameof(LTTngDiskDataCooker.FileEvents)));
            if (fileEvents.Count == 0)
            {
                return;
            }

            var config = new TableConfiguration("EventsBySyscallType")
            {
                Columns = new[]
                {
                    fileEventNameColumn,
                    TableConfiguration.PivotColumn,
                    fileEventThreadIdColumn,
                    fileEventProcessIdColumn,
                    fileEventCommandColumn,
                    fileEventFilePathColumn,
                    fileEventSizeColumn,
                    fileEventDurationColumn,
                    TableConfiguration.GraphColumn,
                    fileEventStartTimeColumn,
                    fileEventEndTimeColumn
                },
            };

            config.AddColumnRole(ColumnRole.StartTime, fileEventStartTimeColumn);
            config.AddColumnRole(ColumnRole.EndTime, fileEventEndTimeColumn);

            var table = tableBuilder.AddTableConfiguration(config)
                                    .SetDefaultTableConfiguration(config)
                                    .SetRowCount(fileEvents.Count);

            table.AddColumn(fileEventNameColumn, Projection.CreateUsingFuncAdaptor((i) => fileEvents[i].Name));
            table.AddColumn(fileEventThreadIdColumn, Projection.CreateUsingFuncAdaptor((i) => fileEvents[i].ThreadId));
            table.AddColumn(fileEventProcessIdColumn, Projection.CreateUsingFuncAdaptor((i) => fileEvents[i].ProcessId));
            table.AddColumn(fileEventCommandColumn, Projection.CreateUsingFuncAdaptor((i) => fileEvents[i].ProcessCommand));
            table.AddColumn(fileEventFilePathColumn, Projection.CreateUsingFuncAdaptor((i) => fileEvents[i].Filepath));
            table.AddColumn(fileEventStartTimeColumn, Projection.CreateUsingFuncAdaptor((i) => fileEvents[i].StartTime));
            table.AddColumn(fileEventEndTimeColumn, Projection.CreateUsingFuncAdaptor((i) => fileEvents[i].EndTime));
            table.AddColumn(fileEventDurationColumn, Projection.CreateUsingFuncAdaptor((i) => fileEvents[i].EndTime - fileEvents[i].StartTime));
            table.AddColumn(fileEventSizeColumn, new FileActivitySizeProjection(Projection.CreateUsingFuncAdaptor((i) => fileEvents[i])));
        }

        public struct FileActivitySizeProjection
            : IProjection<int, Bytes>
        {
            private readonly IProjection<int, IFileEvent> fileActivities;

            public FileActivitySizeProjection(IProjection<int, IFileEvent> fileActivities)
            {
                this.fileActivities = fileActivities;
            }

            public Type SourceType => typeof(int);

            public Type ResultType => typeof(Bytes);

            public Bytes this[int value]
            {
                get
                {
                    return new Bytes(fileActivities[value].Size.Bytes);
                }
            }
        }
    }
}
