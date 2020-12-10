// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using LinuxLogParserCore;
using Microsoft.Performance.SDK;
using Microsoft.Performance.SDK.Extensibility.SourceParsing;
using Microsoft.Performance.SDK.Processing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

namespace LinuxLogParser.CloudInitLog
{
    public class CloudInitLogParser : LogParserBase<CloudInitLogParsedEntry, LogParsedDataKey>
    {
        public override string Id => SourceParserIds.CloudInitLog;
        public override DataSourceInfo DataSourceInfo => dataSourceInfo;

        private static Regex CloudInitRegex =
            new Regex(@"^(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2},\d{3}) - (.*)\[(\w+)\]: (.*)$",
                RegexOptions.Compiled);

        private DataSourceInfo dataSourceInfo;

        public CloudInitLogParser(string[] filePaths) : base(filePaths)
        {
        }

        public override void ProcessSource(
           ISourceDataProcessor<CloudInitLogParsedEntry, LogContext, LogParsedDataKey> dataProcessor,
           ILogger logger, IProgress<int> progress, CancellationToken cancellationToken)
        {
            if (FilePaths.Length == 0)
            {
                return;
            }

            Timestamp startTime = Timestamp.MaxValue;
            Timestamp endTime = Timestamp.MinValue;
            DateTime fileStartTime = default(DateTime);
            long startNanoSeconds = 0;

            foreach (var path in FilePaths)
            {
                ulong lineNumber = 0;
                string line;

                var file = new StreamReader(path);

                var logEntries = new List<LogEntry>();

                while ((line = file.ReadLine()) != null)
                {
                    lineNumber++;

                    var cloudInitMatch = CloudInitRegex.Match(line);
                    if (cloudInitMatch.Success)
                    {
                        DateTime time;
                        if (!DateTime.TryParse(cloudInitMatch.Groups[1].Value.Replace(",", "."), out time))
                        {
                            throw new InvalidOperationException("Time cannot be parsed to DateTime format");
                        }

                        var utcTime = DateTime.FromFileTimeUtc(time.ToFileTimeUtc());  // Need to explicitly say log time is in UTC, otherwise it will be interpreted as local
                        var timeStamp = Timestamp.FromNanoseconds(utcTime.Ticks * 100);

                        if (timeStamp < startTime)
                        {
                            startTime = timeStamp;
                            fileStartTime = utcTime;
                            startNanoSeconds = startTime.ToNanoseconds;
                        }

                        if (timeStamp > endTime)
                        {
                            endTime = timeStamp;
                        }

                        var offsetEventTimestamp = new Timestamp(timeStamp.ToNanoseconds - startNanoSeconds);


                        LogEntry entry = new LogEntry(path, lineNumber, offsetEventTimestamp, cloudInitMatch.Groups[2].Value,
                            cloudInitMatch.Groups[3].Value, cloudInitMatch.Groups[4].Value);
                        dataProcessor.ProcessDataElement(entry, Context, cancellationToken);
                    }
                    else
                    {
                        Debugger.Break();
                    }
                }

                var offsetEndTimestamp = new Timestamp(endTime.ToNanoseconds - startNanoSeconds);

                this.dataSourceInfo = new DataSourceInfo(0, offsetEndTimestamp.ToNanoseconds, fileStartTime);
                Context.UpdateFileMetadata(path, new FileMetadata(lineNumber));
            }
        }
    }
}
