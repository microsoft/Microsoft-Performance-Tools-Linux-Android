// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Performance.SDK.Extensibility;
using Microsoft.Performance.SDK.Processing;
using PerfettoCds.Pipeline.SourceDataCookers;
using PerfettoProcessor;

namespace PerfettoCds.Pipeline.Tables
{
    [Table]
    public class PerfettoPackageTable
    {
        public static TableDescriptor TableDescriptor => new TableDescriptor(
            Guid.Parse("{82B1AF19-B811-4B51-904E-937F3AEBE9EB}"),
            "Packages",
            "Metadata about packages installed on the system",
            "Perfetto - Android",
            defaultLayout: TableLayoutStyle.Table,
            requiredDataCookers: new List<DataCookerPath> { PerfettoPluginConstants.PackageListCookerPath }
        );

        private static readonly ColumnConfiguration PackageNameColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{53AA9D46-D7F1-4053-A16B-E2CC960FF48B}"), "PackageName", "name of the package, e.g. com.google.android.gm"),
            new UIHints { Width = 210 });
        private static readonly ColumnConfiguration UidColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{3C32D709-9DE8-407E-A4D2-F0BECA33C0A9}"), "Uid", "UID processes of this package run as"),
            new UIHints { Width = 210, IsVisible = false });
        private static readonly ColumnConfiguration DebuggableColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{28B2A721-603B-46A7-A7FF-019C46CD7718}"), "Debuggable", "bool whether this app is debuggable"),
            new UIHints { Width = 120 });
        private static readonly ColumnConfiguration ProfileableFromShellColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{8A072ECE-C5C2-417B-8257-1891F32C4DEC}"), "ProfileableFromShell", "bool whether this app is profileable"),
            new UIHints { Width = 210 });
        private static readonly ColumnConfiguration VersionCodeColumn = new ColumnConfiguration(
            new ColumnMetadata(new Guid("{1AC6CF0F-46F9-47B6-B91E-BDF3BC558C6B}"), "VersionCode", "versionCode from the APK"),
            new UIHints { Width = 210 });



        public static bool IsDataAvailable(IDataExtensionRetrieval tableData)
        {
            return tableData.QueryOutput<ProcessedEventData<PerfettoPackageListEvent>>(
                new DataOutputPath(PerfettoPluginConstants.PackageListCookerPath, nameof(PerfettoPackageListCooker.PackageListEvents))).Any();
        }

        public static void BuildTable(ITableBuilder tableBuilder, IDataExtensionRetrieval tableData)
        {
            // Get data from the cooker
            var events = tableData.QueryOutput<ProcessedEventData<PerfettoPackageListEvent>>(
                new DataOutputPath(PerfettoPluginConstants.PackageListCookerPath, nameof(PerfettoPackageListCooker.PackageListEvents)));

            var tableGenerator = tableBuilder.SetRowCount((int)events.Count);
            var baseProjection = Projection.Index(events);

            tableGenerator.AddColumn(PackageNameColumn, baseProjection.Compose(x => x.PackageName));
            tableGenerator.AddColumn(UidColumn, baseProjection.Compose(x => x.Uid));
            tableGenerator.AddColumn(DebuggableColumn, baseProjection.Compose(x => x.Debuggable));
            tableGenerator.AddColumn(ProfileableFromShellColumn, baseProjection.Compose(x => x.ProfileableFromShell));
            tableGenerator.AddColumn(VersionCodeColumn, baseProjection.Compose(x => x.VersionCode));

            // Default
            List<ColumnConfiguration> defaultColumns = new List<ColumnConfiguration>()
            {
                    PackageNameColumn,
                    TableConfiguration.PivotColumn, // Columns before this get pivotted on
                    UidColumn,
                    VersionCodeColumn,
                    ProfileableFromShellColumn,
                    TableConfiguration.GraphColumn,
            };

            var packageListDefaultConfig = new TableConfiguration("Default")
            {
                Columns = defaultColumns,
                ChartType = ChartType.Line
            };

            tableBuilder.AddTableConfiguration(packageListDefaultConfig)
                        .SetDefaultTableConfiguration(packageListDefaultConfig);
        }
    }
}
