// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using LinuxLogParser;
using LinuxLogParser.AndroidLogcat;
using LinuxLogParserCore;
using Microsoft.Performance.SDK;
using Microsoft.Performance.SDK.Extensibility;
using Microsoft.Performance.SDK.Extensibility.DataCooking;
using Microsoft.Performance.SDK.Extensibility.DataCooking.SourceDataCooking;
using System.Collections.Generic;
using System.Threading;

namespace AndroidLogcatMPTAddin
{
    public class AndroidLogcatDataCooker :
        CookedDataReflector,
        ISourceDataCooker<AndroidLogcatLogParsedEntry, LogContext, LogParsedDataKey>
    {
        public const string CookerId = "AndroidLogcatCooker";

        public ReadOnlyHashSet<LogParsedDataKey> DataKeys => new ReadOnlyHashSet<LogParsedDataKey>(
            new HashSet<LogParsedDataKey>(
                new[] {
                    LogParsedDataKey.GeneralLog
                }
            )
        );

        public SourceDataCookerOptions Options => SourceDataCookerOptions.None;

        public string Description => "Parsed information of Android logcat log.";

        public string SourceParserId => Path.SourceParserId;

        public DataCookerPath Path { get; }

        public IReadOnlyDictionary<DataCookerPath, DataCookerDependencyType> DependencyTypes =>
            new Dictionary<DataCookerPath, DataCookerDependencyType>();

        public IReadOnlyCollection<DataCookerPath> RequiredDataCookers => new HashSet<DataCookerPath>();

        public DataProductionStrategy DataProductionStrategy => DataProductionStrategy.PostSourceParsing;

        [DataOutput]
        public AndroidLogcatParsedResult ParsedResult { get; private set; }

        private List<LogEntry> logEntries;
        private List<DurationLogEntry> durationLogEntries;
        private LogContext context;

        public AndroidLogcatDataCooker() : this(DataCookerPath.ForSource(SourceParserIds.AndroidLogcatLog, CookerId))
        {
        }

        private AndroidLogcatDataCooker(DataCookerPath dataCookerPath) : base(dataCookerPath)
        {
            Path = dataCookerPath;
        }

        public void BeginDataCooking(ICookedDataRetrieval dependencyRetrieval, CancellationToken cancellationToken)
        {
            logEntries = new List<LogEntry>();
            durationLogEntries = new List<DurationLogEntry>();
        }

        public DataProcessingResult CookDataElement(AndroidLogcatLogParsedEntry data, LogContext context,
            CancellationToken cancellationToken)
        {
            DataProcessingResult result = DataProcessingResult.Processed;

            if (data is LogEntry logEntry)
            {
                logEntries.Add(logEntry);
                this.context = context;
            }
            else if (data is DurationLogEntry durationLogEntry)
            {
                durationLogEntries.Add(durationLogEntry);
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
            ParsedResult = new AndroidLogcatParsedResult(logEntries, durationLogEntries, context.FileToMetadata);
        }
    }
}
