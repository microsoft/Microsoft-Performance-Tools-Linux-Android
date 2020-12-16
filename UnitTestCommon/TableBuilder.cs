// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Performance.SDK.Processing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace UnitTestCommon
{
    public class TableBuilder : ITableBuilder
    {
        public IEnumerable<TableConfiguration> BuiltInTableConfigurations { get; }
        public TableConfiguration DefaultConfiguration { get; private set; }

        private List<TableConfiguration> tableConfigurations;
        private List<Tuple<string, TableCommandCallback>> tables;

        public TableBuilderWithRowCount TableBuilderWithRowCount;

        public TableBuilder()
        {
            TableBuilderWithRowCount = new TableBuilderWithRowCount();
            tableConfigurations = new List<TableConfiguration>();
            tables = new List<Tuple<string, TableCommandCallback>>();
        }

        public ITableBuilder AddTableCommand(string commandName, TableCommandCallback callback)
        {
            tables.Add(new Tuple<string, TableCommandCallback>(commandName, callback));
            return this;
        }

        public ITableBuilder AddTableConfiguration(TableConfiguration configuration)
        {
            if (tableConfigurations.Any(f => f.Name == configuration.Name))
            {
                throw new Exception("TableConfiguration: Already existing name");
            }

            tableConfigurations.Add(configuration);

            return this;
        }

        public ITableBuilder SetDefaultTableConfiguration(TableConfiguration configuration)
        {
            DefaultConfiguration = configuration;
            return this;
        }

        public ITableBuilderWithRowCount SetRowCount(int numberOfRows)
        {
            TableBuilderWithRowCount.RowCount = numberOfRows;
            return TableBuilderWithRowCount;
        }
    }
}
