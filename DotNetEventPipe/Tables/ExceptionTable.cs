// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;
using Microsoft.Performance.SDK.Processing;
using Utilities;
using Utilities.AccessProviders;
using static Utilities.TimeHelper;

namespace DotNetEventPipe.Tables
{
    //
    // Have the MetadataTable inherit the TableBase class
    //
    [Table]              // A category is optional. It useful for grouping different types of tables
    public sealed class ExceptionTable
        : TraceEventTableBase
    {
        public static readonly TableDescriptor TableDescriptor = new TableDescriptor(
            Guid.Parse("{4975D02A-9950-49B6-847F-C9C0D41DDCBE}"),
            "Exceptions",
            "Exceptions",
            category: ".NET trace (dotnet-trace)");

        public ExceptionTable(IReadOnlyDictionary<string, TraceEventProcessor> traceEventProcessor)
            : base(traceEventProcessor)
        {
        }

        private static readonly ColumnConfiguration exceptionTypeColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{E7CD518A-6644-4E17-9AD8-5A034C3F63A2}"), "ExceptionType", "Exception Type"),
            new UIHints { Width = 305 });

        private static readonly ColumnConfiguration exceptionMessageColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{6A2381EF-6C6F-427F-846F-84D21C7F7793}"), "ExceptionMessage", "Exception Message"),
            new UIHints { Width = 160 });

        private static readonly ColumnConfiguration exceptionEIP = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{9C107E94-1DB5-400F-B36B-0864BA1B9F94}"), "ExceptionEIP", "Exception EIP"),
            new UIHints { Width = 125, CellFormat = ColumnFormats.HexFormat, IsVisible = false });

        private static readonly ColumnConfiguration exceptionHRESULTColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{996C5372-9F87-46A5-9D0D-678DCF37D040}"), "ExceptionHRESULT", "Exception HRESULT"),
            new UIHints { Width = 125, CellFormat = ColumnFormats.HexFormat });

        private static readonly ColumnConfiguration exceptionFlagsColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{5A263CF7-2338-4BC4-9523-FD3EAB8B5AA0}"), "ExceptionFlags", "Exception Flags"),
            new UIHints { Width = 120 });

        private static readonly ColumnConfiguration clrInstanceIDColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{08C5C067-C27A-4542-A761-B1350B0E8DF4}"), "ClrInstanceID", "Clr Instance ID"),
            new UIHints { Width = 90, IsVisible = false });

        private static readonly ColumnConfiguration timestampColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{6056D148-77BC-430A-927D-CF7192C02E45}"), "Timestamp", "The timestamp of the exception"),
            new UIHints { Width = 80 });


        private static readonly ColumnConfiguration countColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{09CCE4A5-A3B0-4C25-B05E-609620702FD7}"), "Count", "The count of samples"),
            new UIHints { 
                Width = 130, 
                AggregationMode = AggregationMode.Sum,
                SortOrder = SortOrder.Descending,
                SortPriority = 0,
            });

        private static readonly ColumnConfiguration hascallStackColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{E2DFB281-9904-4595-AB00-26FD104CAB0D}"), "HasCallstack", "Has Callstack"),
                new UIHints { Width = 40, });

        private static readonly ColumnConfiguration callStackColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{1C9357D2-CA16-439F-A3BE-9E7C5B903CEB}"), "ExStack", "Callstack of the exception"),
                new UIHints { Width = 430, });

        private static readonly ColumnConfiguration threadIdColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{0D6A5367-BF52-49CD-859E-CAC7F7F5C912}"), "ThreadId"),
                new UIHints { Width = 80, });

        private static readonly ColumnConfiguration processColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{BE450391-3E06-45C9-ABCB-6786A6CBAB00}"), "Process"),
                new UIHints { Width = 80, });

        private static readonly ColumnConfiguration processIdColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{E3D33FD4-4120-4EFB-B2B9-16523332B2F5}"), "Process Id"),
                new UIHints { Width = 80, });

        private static readonly ColumnConfiguration cpuColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{23FA8CD4-C253-426A-BADB-1AAE246A7169}"), "CPU"),
                new UIHints { Width = 80, });


        public override void Build(ITableBuilder tableBuilder)
        {
            if (TraceEventProcessor == null || TraceEventProcessor.Count == 0)
            {
                return;
            }

            var firstTraceProcessorEventsParsed = TraceEventProcessor.First().Value;  // First Log
            var exceptions = firstTraceProcessorEventsParsed.GenericEvents.Where(f => f.ProviderName == "Microsoft-Windows-DotNETRuntime" && f.EventName == "Exception/Start").ToArray();

            var tableGenerator = tableBuilder.SetRowCount(exceptions.Length);
            var baseProjection = Projection.Index(exceptions);

            tableGenerator.AddColumn(countColumn, baseProjection.Compose(x => 1));                  // 1 sample
            tableGenerator.AddColumn(exceptionTypeColumn, baseProjection.Compose(x => x.PayloadValues.Length >= 1 ? (string) x.PayloadValues[0] : String.Empty));
            tableGenerator.AddColumn(exceptionMessageColumn, baseProjection.Compose(x => x.PayloadValues.Length >= 2 ? (string) x.PayloadValues[1] : String.Empty));
            tableGenerator.AddColumn(exceptionEIP, baseProjection.Compose(x => x.PayloadValues.Length >= 3 ? (ulong) x.PayloadValues[2] : 0));
            tableGenerator.AddColumn(exceptionHRESULTColumn, baseProjection.Compose(x => x.PayloadValues.Length >= 4 ? (int) x.PayloadValues[3] : 0));
            tableGenerator.AddColumn(exceptionFlagsColumn, baseProjection.Compose(x => x.PayloadValues.Length >= 5 ? (ExceptionThrownFlags) x.PayloadValues[4] : ExceptionThrownFlags.None));
            tableGenerator.AddColumn(clrInstanceIDColumn, baseProjection.Compose(x => x.PayloadValues.Length >= 6 ? (int) x.PayloadValues[5] : 0));
            tableGenerator.AddColumn(processIdColumn, baseProjection.Compose(x => x.ProcessID));
            tableGenerator.AddColumn(processColumn, baseProjection.Compose(x => x.ProcessName));
            tableGenerator.AddColumn(cpuColumn, baseProjection.Compose(x => x.ProcessorNumber));
            tableGenerator.AddColumn(threadIdColumn, baseProjection.Compose(x => x.ThreadID));
            tableGenerator.AddColumn(timestampColumn, baseProjection.Compose(x => x.Timestamp));
            tableGenerator.AddColumn(hascallStackColumn, baseProjection.Compose(x => x.CallStack != null));
            tableGenerator.AddHierarchicalColumn(callStackColumn, baseProjection.Compose(x => x.CallStack), new ArrayAccessProvider<string>());

            var exceptionsTableConfig = new TableConfiguration("Exceptions by Type, Callstack")
            {
                Columns = new ColumnConfiguration[]
                {
                    exceptionTypeColumn,
                    callStackColumn,
                    TableConfiguration.PivotColumn,
                    exceptionMessageColumn,
                    exceptionHRESULTColumn,
                    processColumn,
                    exceptionFlagsColumn,
                    cpuColumn,
                    threadIdColumn,
                    countColumn,
                    exceptionEIP,
                    clrInstanceIDColumn,
                    TableConfiguration.GraphColumn, // Columns after this get graphed
                    timestampColumn,
        }
            };
            exceptionsTableConfig.AddColumnRole(ColumnRole.StartTime, timestampColumn);
            exceptionsTableConfig.AddColumnRole(ColumnRole.EndTime, timestampColumn);

            var table = tableBuilder
            .AddTableConfiguration(exceptionsTableConfig)
            .SetDefaultTableConfiguration(exceptionsTableConfig);
        }
    }
}
