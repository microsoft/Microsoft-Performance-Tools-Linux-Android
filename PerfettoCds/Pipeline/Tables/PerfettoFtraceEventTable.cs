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
using PerfettoCds.Pipeline.DataCookers;

namespace PerfettoCds.Pipeline.Tables
{
    [Table]
    public class PerfettoFtraceEventTable
    {
        // Set some sort of max to prevent ridiculous field counts
        public const int AbsoluteMaxFields = 20;

        public static TableDescriptor TableDescriptor => new TableDescriptor(
            Guid.Parse("{96beb7a0-5a9e-4713-b1f7-4ee74d27851c}"),
            "Perfetto Ftrace Events",
            "All Ftrace events in the Perfetto trace",
            "Perfetto",
            requiredDataCookers: new List<DataCookerPath> { PerfettoPluginConstants.FtraceEventCookerPath }
        );

        // TODO update descriptions
        private static readonly ColumnConfiguration StartTimestampColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{e9675de9-4a76-4bba-a387-169c7ee38425}"), "StartTimestamp", "Start timestamp of the event"),
            new UIHints { Width = 180 });

        private static readonly ColumnConfiguration ProcessNameColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{8027964f-4c41-4309-ada1-b9a40d685b24}"), "ProcessName", "Name of the process that logged the event"),
            new UIHints { Width = 210 });
        
        private static readonly ColumnConfiguration ThreadNameColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{276ab2ad-722c-4a1b-8d9f-dc7b562d3a5c}"), "ThreadName", "Name of the thread that logged the event"),
            new UIHints { Width = 210 });

        private static readonly ColumnConfiguration CpuColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{16b7cf75-de7c-4cb7-9d72-3302a1cdf54f}"), "Cpu", "CPU"),
            new UIHints { Width = 150 });

        private static readonly ColumnConfiguration NameColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{ea581f83-b632-4b5b-9a89-844994f497ca}"), "Name", "Name of the Ftrace event"),
            new UIHints { Width = 120 });


        public static void BuildTable(ITableBuilder tableBuilder, IDataExtensionRetrieval tableData)
        {
            // We dynamically adjust the column headers
            // This is the max number of fields we can expect for this table
            int maxFieldCount = Math.Min(AbsoluteMaxFields, tableData.QueryOutput<int>(
                new DataOutputPath(PerfettoPluginConstants.FtraceEventCookerPath, nameof(PerfettoFtraceEventCooker.MaximumEventFieldCount))));

            // Get data from the cooker
            var events = tableData.QueryOutput<ProcessedEventData<PerfettoFtraceEvent>>(
                new DataOutputPath(PerfettoPluginConstants.FtraceEventCookerPath, nameof(PerfettoFtraceEventCooker.FtraceEvents)));

            // Start construction of the column order. Pivot on process and thread
            List<ColumnConfiguration> allColumns = new List<ColumnConfiguration>() 
            {
                CpuColumn,
                TableConfiguration.PivotColumn, // Columns before this get pivotted
                ProcessNameColumn,
                ThreadNameColumn,
                NameColumn,
            };

            var tableGenerator = tableBuilder.SetRowCount((int)events.Count);
            var eventProjection = new EventProjection<PerfettoFtraceEvent>(events);

            var processNameColumn = new BaseDataColumn<string>(
                ProcessNameColumn,
                eventProjection.Compose((ftraceEvent) => ftraceEvent.ProcessName));
            tableGenerator.AddColumn(processNameColumn);

            var threadNameColumn = new BaseDataColumn<string>(
                ThreadNameColumn,
                eventProjection.Compose((ftraceEvent) => ftraceEvent.ThreadName));
            tableGenerator.AddColumn(threadNameColumn);

            var startTimestampColumn = new BaseDataColumn<Timestamp>(
                StartTimestampColumn,
                eventProjection.Compose((ftraceEvent) => ftraceEvent.StartTimestamp));
            tableGenerator.AddColumn(startTimestampColumn);

            var cpuColumn = new BaseDataColumn<long>(
                CpuColumn,
                eventProjection.Compose((ftraceEvent) => ftraceEvent.Cpu));
            tableGenerator.AddColumn(cpuColumn);

            var nameColumn = new BaseDataColumn<string>(
                NameColumn,
                eventProjection.Compose((ftraceEvent) => ftraceEvent.Name));
            tableGenerator.AddColumn(nameColumn);

            // Add the field columns, with column names depending on the given event
            for (int index = 0; index < maxFieldCount; index++)
            {
                var colIndex = index;  // This seems unncessary but causes odd runtime behavior if not done this way. Compiler is confused perhaps because w/o this func will index=event.FieldNames.Count every time. index is passed as ref but colIndex as value into func
                string fieldName = "Field " + (colIndex + 1);

                var eventFieldNameProjection = eventProjection.Compose((ftraceEvent) => colIndex < ftraceEvent.ArgKeys.Count ? ftraceEvent.ArgKeys[colIndex] : string.Empty);

                // generate a column configuration
                var fieldColumnConfiguration = new ColumnConfiguration(
                        new ColumnMetadata(GenerateGuidFromName(fieldName), fieldName, eventFieldNameProjection, fieldName),
                        new UIHints
                        {
                            IsVisible = true,
                            Width = 150,
                            TextAlignment = TextAlignment.Left,
                        });

                // Add this column to the column order
                allColumns.Add(fieldColumnConfiguration);

                var eventFieldAsStringProjection = eventProjection.Compose((ftraceEvent) => colIndex < ftraceEvent.Values.Count ? ftraceEvent.Values[colIndex] : string.Empty);

                tableGenerator.AddColumn(fieldColumnConfiguration, eventFieldAsStringProjection);
            }

            // Finish the column order with the timestamp columned being graphed
            allColumns.Add(TableConfiguration.GraphColumn); // Columns after this get graphed
            allColumns.Add(StartTimestampColumn);

            var tableConfig = new TableConfiguration("Perfetto Ftrace Events")
            {
                Columns = allColumns,
                Layout = TableLayoutStyle.GraphAndTable
            };
            tableConfig.AddColumnRole(ColumnRole.StartTime, StartTimestampColumn.Metadata.Guid);

            tableBuilder.AddTableConfiguration(tableConfig).SetDefaultTableConfiguration(tableConfig);
        }

        [SuppressMessage("Microsoft.Security.Cryptography", "CA5354:SHA1CannotBeUsed", Justification = "Not a security related usage - just generating probabilistically unique id to identify a column from its name.")]
        private static Guid GenerateGuidFromName(string name)
        {
            // The algorithm below is following the guidance of http://www.ietf.org/rfc/rfc4122.txt
            // Create a blob containing a 16 byte number representing the namespace
            // followed by the unicode bytes in the name.  
            var bytes = new byte[name.Length * 2 + 16];
            uint namespace1 = 0x482C2DB2;
            uint namespace2 = 0xC39047c8;
            uint namespace3 = 0x87F81A15;
            uint namespace4 = 0xBFC130FB;
            // Write the bytes most-significant byte first.  
            for (int i = 3; 0 <= i; --i)
            {
                bytes[i] = (byte)namespace1;
                namespace1 >>= 8;
                bytes[i + 4] = (byte)namespace2;
                namespace2 >>= 8;
                bytes[i + 8] = (byte)namespace3;
                namespace3 >>= 8;
                bytes[i + 12] = (byte)namespace4;
                namespace4 >>= 8;
            }
            // Write out  the name, most significant byte first
            for (int i = 0; i < name.Length; i++)
            {
                bytes[2 * i + 16 + 1] = (byte)name[i];
                bytes[2 * i + 16] = (byte)(name[i] >> 8);
            }

            // Compute the Sha1 hash 
            var sha1 = SHA1.Create();
            byte[] hash = sha1.ComputeHash(bytes);

            // Create a GUID out of the first 16 bytes of the hash (SHA-1 create a 20 byte hash)
            int a = (((((hash[3] << 8) + hash[2]) << 8) + hash[1]) << 8) + hash[0];
            short b = (short)((hash[5] << 8) + hash[4]);
            short c = (short)((hash[7] << 8) + hash[6]);

            c = (short)((c & 0x0FFF) | 0x5000);   // Set high 4 bits of octet 7 to 5, as per RFC 4122
            Guid guid = new Guid(a, b, c, hash[8], hash[9], hash[10], hash[11], hash[12], hash[13], hash[14], hash[15]);
            return guid;
        }

        public struct EventProjection<T>
            : IProjection<int, T>
        {
            private readonly ProcessedEventData<T> events;

            public EventProjection(ProcessedEventData<T> events)
            {
                this.events = events;
            }

            public Type SourceType => typeof(int);

            public Type ResultType => typeof(T);

            public T this[int value] => this.events[(uint)value];
        }
    }
}
