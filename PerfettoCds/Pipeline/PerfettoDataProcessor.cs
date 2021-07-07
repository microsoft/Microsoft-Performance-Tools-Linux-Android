// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Microsoft.Performance.SDK.Extensibility;
using Microsoft.Performance.SDK.Extensibility.SourceParsing;
using Microsoft.Performance.SDK.Processing;
using System;
using System.Collections.Generic;
using PerfettoCds.Pipeline.Events;

namespace PerfettoCds
{
    /// <summary>
    ///     This class only delegates work off to the source parser, so there's no logic inside of it.
    ///     <para/>
    ///     Since our table has required data cookers, the SDK takes care of making sure it
    ///     gets built.
    /// </summary>
    public class PerfettoDataProcessor : CustomDataProcessorBaseWithSourceParser<PerfettoSqlEventKeyed, PerfettoSourceParser, string>
    {
        internal PerfettoDataProcessor(
            ISourceParser<PerfettoSqlEventKeyed, PerfettoSourceParser, string> sourceParser,
            ProcessorOptions options,
            IApplicationEnvironment applicationEnvironment,
            IProcessorEnvironment processorEnvironment,
            IReadOnlyDictionary<TableDescriptor, Action<ITableBuilder, IDataExtensionRetrieval>> allTablesMapping,
            IEnumerable<TableDescriptor> metadataTables)
            : base(sourceParser, options, applicationEnvironment, processorEnvironment, allTablesMapping, metadataTables)
        {
        }
    }
}
