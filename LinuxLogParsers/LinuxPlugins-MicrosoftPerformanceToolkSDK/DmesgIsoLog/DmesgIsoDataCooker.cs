// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using LinuxLogParser;
using LinuxLogParser.DmesgIsoLog;
using LinuxLogParserCore;
using Microsoft.Performance.SDK;
using Microsoft.Performance.SDK.Extensibility;
using Microsoft.Performance.SDK.Extensibility.DataCooking;
using Microsoft.Performance.SDK.Extensibility.DataCooking.SourceDataCooking;
using System.Collections.Generic;
using System.Threading;

namespace DmesgIsoMPTAddin
{
    public class DmesgIsoDataCooker :
        CookedDataReflector,
        ISourceDataCooker<DmesgIsoLogParsedEntry, LogContext, LogParsedDataKey>
    {
        public const string CookerId = "DmesgIsoCooker";

        public ReadOnlyHashSet<LogParsedDataKey> DataKeys => new ReadOnlyHashSet<LogParsedDataKey>(
            new HashSet<LogParsedDataKey>(
                new[] {
                    LogParsedDataKey.GeneralLog
                }
            )
        );

        public SourceDataCookerOptions Options => SourceDataCookerOptions.None;

        public string Description => "Parsed information of Dmesg log.";

        public string SourceParserId => Path.SourceParserId;

        public DataCookerPath Path { get; }

        public IReadOnlyDictionary<DataCookerPath, DataCookerDependencyType> DependencyTypes =>
            new Dictionary<DataCookerPath, DataCookerDependencyType>();

        public IReadOnlyCollection<DataCookerPath> RequiredDataCookers => new HashSet<DataCookerPath>();

        public DataProductionStrategy DataProductionStrategy => DataProductionStrategy.PostSourceParsing;

        [DataOutput]
        public DmesgIsoLogParsedResult ParsedResult { get; private set; }

        private List<LogEntry> logEntries;
        private LogContext context;

        public DmesgIsoDataCooker() : this(DataCookerPath.ForSource(SourceParserIds.DmesgIsoLog, CookerId))
        {
        }

        private DmesgIsoDataCooker(DataCookerPath dataCookerPath) : base(dataCookerPath)
        {
            Path = dataCookerPath;
        }

        public void BeginDataCooking(ICookedDataRetrieval dependencyRetrieval, CancellationToken cancellationToken)
        {
            logEntries = new List<LogEntry>();
        }

        public DataProcessingResult CookDataElement(DmesgIsoLogParsedEntry data, LogContext context,
            CancellationToken cancellationToken)
        {
            DataProcessingResult result = DataProcessingResult.Processed;

            if (data is LogEntry logEntry)
            {
                logEntries.Add(logEntry);
                this.context = context;
            }
            else
            {
                result = DataProcessingResult.Ignored;
            }

            return result;
        }

        public void EndDataCooking(CancellationToken cancellationToken)
        {
            ParsedResult = new DmesgIsoLogParsedResult(logEntries, context.FileToMetadata);
        }
    }
}
