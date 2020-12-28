// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Performance.SDK.Processing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace UnitTestCommon
{
    public class TableBuilderWithRowCount : ITableBuilderWithRowCount
    {
        public int RowCount { get; set; }

        public IReadOnlyCollection<IDataColumn> Columns
        {
            get { return _columns; }
        }

        private List<IDataColumn> _columns { get; set; }

        public TableBuilderWithRowCount()
        {
            _columns = new List<IDataColumn>();
        }

        public ITableBuilderWithRowCount AddColumn(IDataColumn column)
        {
            if (_columns.Any(f => f.Configuration.Metadata.Name == column.Configuration.Metadata.Name))
            {
                throw new Exception("AddColumn: Already existing name");
            }
            if (_columns.Any(f => f.Configuration.Metadata.Guid == column.Configuration.Metadata.Guid))
            {
                throw new Exception("AddColumn: Already existing Guid");
            }
            _columns.Add(column);

            return this;
        }

        public ITableBuilderWithRowCount ReplaceColumn(IDataColumn oldColumn, IDataColumn newColumn)
        {
            _columns[_columns.IndexOf(oldColumn)] = newColumn;
            return this;
        }

        public ITableBuilderWithRowCount SetTableRowDetailsGenerator(Func<int, IEnumerable<TableRowDetailEntry>> generator)
        {
            return this;
        }
    }
}
