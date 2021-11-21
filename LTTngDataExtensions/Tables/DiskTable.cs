// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using LTTngCds.CookerData;
using LTTngDataExtensions.DataOutputTypes;
using LTTngDataExtensions.SourceDataCookers.Disk;
using Microsoft.Performance.SDK;
using Microsoft.Performance.SDK.Extensibility;
using Microsoft.Performance.SDK.Processing;

namespace LTTngDataExtensions.Tables
{
    [Table]
    [RequiresSourceCooker(LTTngConstants.SourceId, LTTngDiskDataCooker.Identifier)]
    ///[PrebuiltConfigurationsFilePath("Resources\\DiskActivityPrebuiltConfiguration.json")]
    public static class DiskTable
    {
        public static TableDescriptor TableDescriptor = new TableDescriptor(
            Guid.Parse("{B18A134C-0B62-4C02-B729-6579C8223AB3}"),
            "Disk",
            "Disk Activity",
            "Linux LTTng",
            defaultLayout: TableLayoutStyle.GraphAndTable);

        private static readonly ColumnConfiguration deviceIdColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{EC918167-5A00-467A-ABE6-F4C2E14B5C31}"), "Device Id"));

        private static readonly ColumnConfiguration deviceNameColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{0DF88D6A-2583-460B-B98B-13079DF731DA}"), "Device Name"));

        private static readonly ColumnConfiguration sectorNumberColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{8C1DF470-BC47-43BC-BF35-68FBADD725CF}"), "Sector Number"));

        private static readonly ColumnConfiguration insertTimeColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{F057FEE1-1DDF-419A-A7C2-150929825BC5}"), "Insert Time"));

        private static readonly ColumnConfiguration issueTimeColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{B9515398-B00F-4CA4-819F-A77292F97CB4}"), "Issue Time"));

        private static readonly ColumnConfiguration ioTimeAvgColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{985C8096-6A60-4A59-8BEF-17335C7896F5}"), "IO Time"),
                new UIHints
                {
                    Width = 120,
                    IsVisible = true,
                    AggregationMode = AggregationMode.Average,
                    SortPriority = 0,
                    SortOrder = SortOrder.Descending,
                    CellFormat = TimestampFormatter.FormatMillisecondsGrouped,
                });

        private static readonly ColumnConfiguration ioTimeMaxColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{985C8096-6A60-4A59-8BEF-17335C7896F5}"), "IO Time"),
                new UIHints
                {
                    Width = 120,
                    IsVisible = true,
                    AggregationMode = AggregationMode.Max,
                    SortPriority = 0,
                    SortOrder = SortOrder.Descending,
                    CellFormat = TimestampFormatter.FormatMillisecondsGrouped,
                });

        private static readonly ColumnConfiguration ioTimeSumColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{985C8096-6A60-4A59-8BEF-17335C7896F5}"), "IO Time"),
                new UIHints
                {
                    Width = 120,
                    IsVisible = true,
                    AggregationMode = AggregationMode.Sum,
                    SortPriority = 0,
                    SortOrder = SortOrder.Descending,
                    CellFormat = TimestampFormatter.FormatMillisecondsGrouped,
                });

        private static readonly ColumnConfiguration weightedIOTimeColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{DB9C35D9-693A-43DF-9BC0-0E54DA389144}"), "Time Percent", "Percent of viewport range time that is consumed by this activity.") { IsPercent = true },
                new UIHints
                {
                    Width = 80,
                    IsVisible = true,
                    AggregationMode = AggregationMode.Sum,
                    TextAlignment = TextAlignment.Right,
                    CellFormat = ColumnFormats.PercentFormat,
                });

        private static readonly ColumnConfiguration completeTimeColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{9C0F7BE4-90D9-4098-ABCB-AD767329C65B}"), "Complete Time"));

        private static readonly ColumnConfiguration filepathColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{D0642DC5-E486-469F-991A-70EBC63F4056}"), "Filepath"));

        private static readonly ColumnConfiguration threadIdColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{9007B743-910B-4C6C-92BE-BCF07719DFAF}"), "Thread Id"));

        private static readonly ColumnConfiguration processIdColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{A19BD56B-7BC7-4D17-98C0-5B097663B140}"), "Process Id"));

        private static readonly ColumnConfiguration commandColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{4306F58E-327D-4C86-979C-4094769DA145}"), "Thread's Command"),
                new UIHints { Width = 80, });

        private static readonly ColumnConfiguration errorColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{BF7D9C8B-600B-4FFC-A016-2B4F59B9DDF6}"), "Error"));

        private static readonly ColumnConfiguration sizeColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{BDE426C2-1CFD-425B-8AD6-37378DAFFC06}"), "Size"),
                new UIHints { Width = 80, IsVisible = true, AggregationMode = AggregationMode.Sum, CellFormat = "B" });

        private static readonly ColumnConfiguration diskOffsetColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{C39E3639-31A5-42E0-86D5-9CDAF4E57CEA}"), "Disk Offset"),
                new UIHints { Width = 80, IsVisible = true, CellFormat = "B" });

        private static readonly ColumnConfiguration countColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{d07a9720-6226-4742-a763-0df601ba5dcc}"), "Count"),
                new UIHints { Width = 80, IsVisible = false });

        private static readonly ColumnConfiguration countColumn_IOSize8kb =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{e58ee65c-8233-4927-b252-efa79433f37a}"), "Count_IOSize_8kb"),
                new UIHints { Width = 80, IsVisible = false });

        private static readonly ColumnConfiguration countColumn_IOSize256kb =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{1e42f42f-5848-4f23-8c9f-51b5f91eea60}"), "Count_IOSize_256kb"),
                new UIHints { Width = 80, IsVisible = false });

        private static readonly ColumnConfiguration ioTimeIsValidColumnConfiguration =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{7C6FFCCE-E80D-4602-B61A-E06C198A85FC}"), "Valid IO Time"),
                new UIHints { Width = 80, IsVisible = false });

        private static readonly ColumnConfiguration clippedTimestampDeltaColumnConfiguration =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{6F229839-2D61-4780-858B-6574E73E43F9}"), "Time Delta"),
                new UIHints { Width = 80, IsVisible = false });

        public static void BuildTable(ITableBuilder tableBuilder, IDataExtensionRetrieval tableData)
        {
            var diskEvents = tableData.QueryOutput<IReadOnlyList<IDiskActivity>>(
                DataOutputPath.ForSource(LTTngConstants.SourceId, LTTngDiskDataCooker.Identifier, nameof(LTTngDiskDataCooker.DiskActivity)));
            if (diskEvents.Count == 0)
            {
                return;
            }

            var iosByDeviceThCmdConfig = new TableConfiguration("IOs by Device, ThreadId, Command")
            {
                Columns = new[]
                {
                    deviceIdColumn,
                    deviceNameColumn,
                    threadIdColumn,
                    commandColumn,
                    TableConfiguration.PivotColumn,
                    processIdColumn,
                    filepathColumn,
                    ioTimeSumColumn,
                    sectorNumberColumn,
                    diskOffsetColumn,
                    sizeColumn,
                    errorColumn,
                    TableConfiguration.GraphColumn,
                    insertTimeColumn,
                    issueTimeColumn,
                    completeTimeColumn
                },
            };

            var ioTimesByDevFileConfig = new TableConfiguration("IOTime by Device, FilePath")
            {
                Columns = new[]
                {
                    deviceIdColumn,
                    deviceNameColumn,
                    TableConfiguration.PivotColumn,
                    filepathColumn,
                    threadIdColumn,
                    processIdColumn,
                    commandColumn,
                    sectorNumberColumn,
                    diskOffsetColumn,
                    sizeColumn,
                    errorColumn,
                    insertTimeColumn,
                    issueTimeColumn,
                    completeTimeColumn,
                    TableConfiguration.GraphColumn,
                    ioTimeAvgColumn,
                    ioTimeMaxColumn
                },
            };

            iosByDeviceThCmdConfig.AddColumnRole(ColumnRole.StartTime, insertTimeColumn);
            // config.AddColumnRole(ColumnRole.EndTime, completeTimeColumn);
            iosByDeviceThCmdConfig.AddColumnRole(ColumnRole.ResourceId, deviceIdColumn); // We have had past behavior where specifying this causes DevId 0 not to show?

            var table = tableBuilder.AddTableConfiguration(iosByDeviceThCmdConfig)
                                    .AddTableConfiguration(ioTimesByDevFileConfig)
                                    .SetDefaultTableConfiguration(iosByDeviceThCmdConfig)
                                    .SetRowCount((int)diskEvents.Count);

            var diskActivities = Projection.CreateUsingFuncAdaptor((i) => diskEvents[i]);

            var defaultTime = default(Timestamp);


            var ioStartTimeProjection = diskActivities.Compose(da => da.InsertTime ?? defaultTime);
            var ioEndTimeProjection = diskActivities.Compose(da => da.CompleteTime ?? defaultTime);
            var validIoTimeProjection =
                diskActivities.Compose(da => da.InsertTime.HasValue && da.CompleteTime.HasValue);

            table.AddColumn(deviceIdColumn, diskActivities.Compose(da => da.DeviceId));
            table.AddColumn(deviceNameColumn, diskActivities.Compose(da => da.DeviceName));
            table.AddColumn(filepathColumn, diskActivities.Compose(da => da.Filepath));
            table.AddColumn(threadIdColumn, diskActivities.Compose(da => da.ThreadId));
            table.AddColumn(processIdColumn, diskActivities.Compose(da => da.ProcessId));
            table.AddColumn(commandColumn, diskActivities.Compose(da => da.ProcessCommand));
            table.AddColumn(errorColumn, diskActivities.Compose(da => da.Error));
            table.AddColumn(ioTimeIsValidColumnConfiguration, validIoTimeProjection);
            table.AddColumn(sectorNumberColumn, diskActivities.Compose(da => da.SectorNumber));
            // todo:can we pick up the sector size from the trace?
            table.AddColumn(diskOffsetColumn, diskActivities.Compose(da => new Bytes(da.SectorNumber * 512)));
            table.AddColumn(insertTimeColumn, ioStartTimeProjection);
            table.AddColumn(issueTimeColumn, diskActivities.Compose(da => da.IssueTime ?? defaultTime));
            table.AddColumn(completeTimeColumn, ioEndTimeProjection);

            var diskActivitiesProj = diskActivities.Compose((da) =>
            {
                if (da.CompleteTime.HasValue)
                {
                    if (da.InsertTime.HasValue)
                    {
                        return da.CompleteTime.Value - da.InsertTime.Value;
                    }
                }

                return TimestampDelta.Zero;
            });

            table.AddColumn(ioTimeAvgColumn, diskActivitiesProj);

            {
                IProjection<int, Timestamp> viewportClippedStartTimeColumn =
                    Projection.ClipTimeToVisibleDomain.Create(ioStartTimeProjection);

                IProjection<int, Timestamp> viewportClippedEndTimeColumn =
                    Projection.ClipTimeToVisibleDomain.Create(ioEndTimeProjection);

                // Timestamp delta for the given disk activity during the viewport time range.
                IProjection<int, TimestampDelta> clippedTimeDeltaColumn = Projection.Select(
                    viewportClippedEndTimeColumn,
                    viewportClippedStartTimeColumn,
                    new ReduceTimeSinceLastDiff(validIoTimeProjection));

                table.AddColumn(clippedTimestampDeltaColumnConfiguration, clippedTimeDeltaColumn);

                // Percent of time consumed by the timestamp delta in the current viewport.
                /* IProjection<int, double> ioTimeWeightPercentColumn =
                     Projection.ClipTimeToVisibleDomain.CreatePercent(clippedTimeDeltaColumn);*/

                /// table.AddColumn(weightedIOTimeColumn, ioTimeWeightPercentColumn);
            }

            table.AddColumn(sizeColumn, new DiskActivitySizeProjection(diskActivities));

            // IOCount with no restriction on IOSize - Used to enable some of the graphs in analyzer
            table.AddColumn(countColumn, Projection.Constant<int>(1));

            // todo: should we move the Azure specific columns here?

            // IOCount when IOSize is 8KB (such as in Azure local SSD throttling)
            table.AddColumn(countColumn_IOSize8kb, diskActivities.Compose(da => da.Size.HasValue && da.Size.Value.Bytes > 0 ? Math.Ceiling((float)da.Size.Value.Bytes / (8 * 1024)) : 1));

            // IOCount when IOSize is 256KB (such as in Azure XStore throttling)
            table.AddColumn(countColumn_IOSize256kb, diskActivities.Compose(da => da.Size.HasValue && da.Size.Value.Bytes > 0 ? Math.Ceiling((float)da.Size.Value.Bytes / (256 * 1024)) : 1));
        }

        private struct DiskActivitySizeProjection
            : IProjection<int, Bytes>
        {
            private readonly IProjection<int, IDiskActivity> diskActivities;

            public DiskActivitySizeProjection(IProjection<int, IDiskActivity> diskActivities)
            {
                this.diskActivities = diskActivities;
            }

            public Type SourceType => typeof(int);

            public Type ResultType => typeof(Bytes);

            public Bytes this[int value]
            {
                get
                {
                    return diskActivities[value].Size.HasValue
                        ? new Bytes(diskActivities[value].Size.Value.Bytes)
                        : default(Bytes);
                }
            }
        }

        private struct ReduceTimeSinceLastDiff
            : IFunc<int, Timestamp, Timestamp, TimestampDelta>
        {
            private readonly IProjection<int, bool> validIoTimeProjection;

            public ReduceTimeSinceLastDiff(IProjection<int, bool> validIoTimeProjection)
            {
                this.validIoTimeProjection = validIoTimeProjection;
            }

            public TimestampDelta Invoke(int value, Timestamp timeSinceLast1, Timestamp timeSinceLast2)
            {
                if (this.validIoTimeProjection[value])
                {
                    return timeSinceLast1 - timeSinceLast2;
                }

                return TimestampDelta.Zero;
            }
        }
    }
}