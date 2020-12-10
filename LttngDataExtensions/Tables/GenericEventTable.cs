// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using LttngDataExtensions.DataOutputTypes;
using Microsoft.Performance.SDK;
using Microsoft.Performance.SDK.Extensibility;
using Microsoft.Performance.SDK.Processing;

namespace LttngDataExtensions.Tables
{
    [Table]
    [RequiresCooker("Lttng/GenericEvents")]
    [PrebuiltConfigurationsFilePath("Resources\\GenericEventTablePrebuiltConfigurations.json")]
    public class GenericEventTable
    {
        public static TableDescriptor TableDescriptor = new TableDescriptor(
            Guid.Parse("{F5EAF336-EA19-487D-9C1F-527812B02F30}"),
            "Generic Events",
            "All events in the LTTNG trace",
            "Linux LTTng");

        private static readonly ColumnConfiguration eventNameColumnConfig = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{8132DED0-8FE7-4533-B139-4C81133A7BCD}"), "Name"),
            new UIHints
            {
                IsVisible = true,
                Width = 200,
            });

        private static readonly ColumnConfiguration eventTimestampColumnConfig = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{F74E122E-2358-4C9C-B9F7-76668B7AD957}"), "Timestamp"),
            new UIHints
            {
                IsVisible = true,
                Width = 100,
            });

        private static readonly ColumnConfiguration eventIdColumnConfig = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{F81236AC-45B0-4695-95DA-4739A8DD0147}"), "Id"),
            new UIHints
            {
                IsVisible = true,
                Width = 80,
            });

        private static readonly ColumnConfiguration cpuIdColumnConfig = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{F1DBA380-E2F3-47D2-8E5E-D753B28D13DD}"), "CPU"),
            new UIHints
            {
                IsVisible = true,
                Width = 80,
            });

        private static readonly ColumnConfiguration discardedEventsColumnConfig = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{877222E9-BF0E-49FF-9506-45091A8C518B}"), "Discarded Events Count"),
            new UIHints
            {
                IsVisible = true,
                Width = 80,
            });

        private static readonly ColumnConfiguration countColumnConfig = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{1D8946DA-567E-4A32-BB46-EB90727DA59C}"), "Count"),
            new UIHints
            {
                IsVisible = true,
                Width = 80,
                AggregationMode = AggregationMode.Sum,
            });

        public static void BuildTable(ITableBuilder tableBuilder, IDataExtensionRetrieval tableData)
        {
            int maximumFieldCount = tableData.QueryOutput<int>(
                DataOutputPath.Create("Lttng/GenericEvents/MaximumEventFieldCount"));

            var events = tableData.QueryOutput<ProcessedEventData<LttngGenericEvent>>(
                DataOutputPath.Create("Lttng/GenericEvents/Events"));

            var tableGenerator = tableBuilder.SetRowCount((int)events.Count);

            var genericEventProjection = new GenericEventProjection(events);

            var eventNameColumn = new BaseDataColumn<string>(
                eventNameColumnConfig, 
                genericEventProjection.Compose((genericEvent) => genericEvent.EventName));
            tableGenerator.AddColumn(eventNameColumn);

            var eventIdColumn = new BaseDataColumn<uint>(
                eventIdColumnConfig, 
                genericEventProjection.Compose((genericEvent) => genericEvent.Id));
            tableGenerator.AddColumn(eventIdColumn);

            var cpuIdColumn = new BaseDataColumn<uint>(
                cpuIdColumnConfig,
                genericEventProjection.Compose((genericEvent) => genericEvent.CpuId));
            tableGenerator.AddColumn(cpuIdColumn);

            var discardedEventsColumn = new BaseDataColumn<uint>(
                discardedEventsColumnConfig,
                genericEventProjection.Compose((genericEvent) => genericEvent.DiscardedEvents));
            tableGenerator.AddColumn(discardedEventsColumn);

            var eventTimestampColumn = new BaseDataColumn<Timestamp>(
                eventTimestampColumnConfig,
                genericEventProjection.Compose((genericEvent) => genericEvent.Timestamp));
            tableGenerator.AddColumn(eventTimestampColumn);

            tableGenerator.AddColumn(countColumnConfig, Projection.Constant(1));

            // Add the field columns, with column names depending on the given event
            for (int columnIndex = 0; columnIndex < maximumFieldCount; columnIndex++)
            {
                string fieldName = "Field " + (columnIndex + 1);

                var genericEventFieldNameProjection = genericEventProjection.Compose((genericEvent) => columnIndex < genericEvent.FieldNames.Count ? genericEvent.FieldNames[columnIndex] : string.Empty);

            // generate a column configuration
            var fieldColumnConfiguration = new ColumnConfiguration(
                    new ColumnMetadata(GenerateGuidFromName(fieldName), fieldName, genericEventFieldNameProjection, fieldName),
                    new UIHints
                    {
                        IsVisible = true,
                        Width = 80,
                        TextAlignment = TextAlignment.Left,
                    });

                var genericEventFieldAsStringProjection = genericEventProjection.Compose((genericEvent) => columnIndex < genericEvent.FieldNames.Count ? genericEvent.FieldValues[columnIndex] : string.Empty);

                tableGenerator.AddColumn(fieldColumnConfiguration, genericEventFieldAsStringProjection);
            }

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
    }

    public struct GenericEventProjection
        : IProjection<int, LttngGenericEvent>
    {
        private readonly ProcessedEventData<LttngGenericEvent> genericEvents;

        public GenericEventProjection(ProcessedEventData<LttngGenericEvent> genericEvents)
        {
            this.genericEvents = genericEvents;
        }

        public Type SourceType => typeof(int);

        public Type ResultType => typeof(LttngGenericEvent);

        public LttngGenericEvent this[int value] => this.genericEvents[(uint)value];
    }
}