// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using LTTngDataExtensions.SourceDataCookers.Syscall;
using Microsoft.Performance.SDK.Extensibility;
using Microsoft.Performance.SDK.Processing;

namespace LTTngDataExtensions.Tables
{
    [Table]
    [PrebuiltConfigurationsFilePath("Resources\\SyscallTablePrebuiltConfigurations.json")]
    [RequiresCooker(LTTngSyscallDataCooker.CookerPath)]
    public class SyscallTable
    {
        public static TableDescriptor TableDescriptor = new TableDescriptor(
            Guid.Parse("{52A9B61F-DEB5-4F3D-A5E8-210ABC0022C0}"),
            "Syscalls",
            "Syscalls History",
            "Linux LTTng");

        private static readonly ColumnConfiguration syscallNameColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{BB6ADD3C-CECB-4338-8FC2-079397839233}"), "Syscall Name"),
                new UIHints { Width = 80, });
        private static readonly ColumnConfiguration syscallNumberColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{A26ECB48-56A0-4A98-8ABE-48C3015B1637}"), "Syscall Number"),
                new UIHints { Width = 80, });

        private static readonly ColumnConfiguration syscallStartTimeColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{F8BBE984-0241-4DAC-99F4-A5D07DF0571A}"), "Start Time"),
                new UIHints { Width = 80, });

        private static readonly ColumnConfiguration syscallEndTimeColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{4F3066C1-C66E-4E94-847C-5D36626DA824}"), "End Time"),
                new UIHints { Width = 80, });

        private static readonly ColumnConfiguration syscallDurationColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{2C63CD3D-985A-4F06-AE79-042BC3B75C73}"), "Duration"),
                new UIHints { Width = 80, CellFormat = "ms" });

        private static readonly ColumnConfiguration syscallThreadIdColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{CE4C85B3-86D2-4BE7-8B13-8E9BA9AAFF7B}"), "Thread Id"),
                new UIHints { Width = 80, });

        private static readonly ColumnConfiguration syscallProcessIdColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{87E7B112-81B2-4CED-BE22-2A9744DD5566}"), "Process Id"),
                new UIHints { Width = 80, });

        private static readonly ColumnConfiguration syscallCommandColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{0B2DC181-B52A-4091-81B7-E930238607BE}"), "Thread's Command"),
                new UIHints { Width = 80, });

        private static readonly ColumnConfiguration syscallArgumentsColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{29D03695-73CE-4ACC-9D97-5164BF86BB49}"), "Arguments"),
                new UIHints { Width = 80, });

        private static readonly ColumnConfiguration syscallReturnValueColumn =
            new ColumnConfiguration(
                new ColumnMetadata(new Guid("{9721F620-CCC6-40E3-BB26-EBE522F2FCE7}"), "Return Value"),
                new UIHints { Width = 80, });

        public static void BuildTable(ITableBuilder tableBuilder, IDataExtensionRetrieval tableData)
        {
            var syscalls = tableData.QueryOutput<IReadOnlyList<ISyscall>>(
                DataOutputPath.Create(LTTngSyscallDataCooker.CookerPath+'/'+nameof(LTTngSyscallDataCooker.Syscalls)));
            if (syscalls.Count == 0)
            {
                return;
            }

            var defaultConfig = new TableConfiguration("Individual Syscalls")
            {
                Columns = new[]
                {
                    syscallNumberColumn,
                    TableConfiguration.PivotColumn,
                    syscallNameColumn,
                    syscallDurationColumn,
                    syscallArgumentsColumn,
                    syscallReturnValueColumn,
                    syscallThreadIdColumn,
                    syscallCommandColumn,
                    syscallProcessIdColumn,
                    TableConfiguration.GraphColumn,
                    syscallStartTimeColumn,
                    syscallEndTimeColumn
                },
                Layout = TableLayoutStyle.GraphAndTable,
            };

            defaultConfig.AddColumnRole(ColumnRole.StartTime, syscallStartTimeColumn);
            defaultConfig.AddColumnRole(ColumnRole.EndTime, syscallEndTimeColumn);

            var table = tableBuilder.AddTableConfiguration(defaultConfig)
                                    .SetDefaultTableConfiguration(defaultConfig)
                                    .SetRowCount(syscalls.Count);

            table.AddColumn(syscallNameColumn, Projection.CreateUsingFuncAdaptor((i) => syscalls[i].Name));
            table.AddColumn(syscallThreadIdColumn, Projection.CreateUsingFuncAdaptor((i) => syscalls[i].ThreadId));
            table.AddColumn(syscallProcessIdColumn, Projection.CreateUsingFuncAdaptor((i) => syscalls[i].ProcessId));
            table.AddColumn(syscallCommandColumn, Projection.CreateUsingFuncAdaptor((i) => syscalls[i].ProcessCommand));
            table.AddColumn(syscallNumberColumn, Projection.CreateUsingFuncAdaptor((i) => i+1));
            table.AddColumn(syscallStartTimeColumn, Projection.CreateUsingFuncAdaptor((i) => syscalls[i].StartTime));
            table.AddColumn(syscallEndTimeColumn, Projection.CreateUsingFuncAdaptor((i) => syscalls[i].EndTime));
            table.AddColumn(syscallDurationColumn, Projection.CreateUsingFuncAdaptor((i) => syscalls[i].EndTime - syscalls[i].StartTime));
            table.AddColumn(syscallReturnValueColumn, Projection.CreateUsingFuncAdaptor((i) => syscalls[i].ReturnValue));
            table.AddColumn(syscallArgumentsColumn, Projection.CreateUsingFuncAdaptor((i) => syscalls[i].Arguments));
        }
    }
}
