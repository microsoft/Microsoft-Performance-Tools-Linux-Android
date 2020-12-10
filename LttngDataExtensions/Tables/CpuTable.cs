// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using LttngDataExtensions.DataOutputTypes;
using Microsoft.Performance.SDK.Extensibility;
using Microsoft.Performance.SDK.Processing;

namespace LttngDataExtensions.Tables
{
    [Table]
    [RequiresCooker("Lttng/CpuDataCooker")]
    [PrebuiltConfigurationsFilePath("Resources\\CpuTablePrebuiltConfigurations.json")]
    public static class CpuTable
    {
        public static TableDescriptor TableDescriptor = new TableDescriptor(
            Guid.Parse("{90AD59C8-63FF-4EBB-8B5E-DD89252C4F75}"),
            "CPU",
            "Context switches");

        private static readonly ColumnConfiguration oldThreadIdColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{A4792E89-2E22-4CBB-976D-1A68640F114F}"), "Old Thread Id"));

        private static readonly ColumnConfiguration oldImageNameColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{8C1DF470-BC47-43BC-BF35-68FBADD725CF}"), "Old Image Name"));

        private static readonly ColumnConfiguration oldPriorityColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{F057FEE1-1DDF-419A-A7C2-150929825BC5}"), "Old Priority"));

        private static readonly ColumnConfiguration newThreadIdColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{B9515398-B00F-4CA4-819F-A77292F97CB4}"), "New Thread Id"));

        private static readonly ColumnConfiguration newImageNameColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{9C0F7BE4-90D9-4098-ABCB-AD767329C65B}"), "New Image Name"));

        private static readonly ColumnConfiguration newPriorityColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{BDE426C2-1CFD-425B-8AD6-37378DAFFC06}"), "New Priority"));

        private static readonly ColumnConfiguration timestampColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{7EF03A70-E787-473D-AC54-9DBA1D8682B1}"), "Switch Time"));

        public static void BuildTable(ITableBuilder tableBuilder, IDataExtensionRetrieval tableData)
        {
            var contextSwitches = tableData.QueryOutput<ProcessedEventData<IContextSwitch>>(
                DataOutputPath.Create("Lttng/CpuDataCooker/ContextSwitches"));
            if (contextSwitches.Count == 0)
            {
                return;
            }

            var table = tableBuilder.SetRowCount((int)contextSwitches.Count);

            table.AddColumn(oldThreadIdColumn, Projection.CreateUsingFuncAdaptor((i) => contextSwitches[(uint)i].SwitchOut.ThreadId));
            table.AddColumn(oldImageNameColumn, Projection.CreateUsingFuncAdaptor((i) => contextSwitches[(uint)i].SwitchOut.ImageName));
            table.AddColumn(oldPriorityColumn, Projection.CreateUsingFuncAdaptor((i) => contextSwitches[(uint)i].SwitchOut.Priority));
            table.AddColumn(newThreadIdColumn, Projection.CreateUsingFuncAdaptor((i) => contextSwitches[(uint)i].SwitchIn.ThreadId));
            table.AddColumn(newImageNameColumn, Projection.CreateUsingFuncAdaptor((i) => contextSwitches[(uint)i].SwitchIn.ImageName));
            table.AddColumn(newPriorityColumn, Projection.CreateUsingFuncAdaptor((i) => contextSwitches[(uint)i].SwitchIn.Priority));
            table.AddColumn(timestampColumn, Projection.CreateUsingFuncAdaptor((i) => contextSwitches[(uint)i].Timestamp));
        }

    }
}