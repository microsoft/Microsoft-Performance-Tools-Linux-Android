// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using LinuxLogParserCore;
using Microsoft.Performance.SDK;
using Microsoft.Performance.SDK.Extensibility.SourceParsing;
using Microsoft.Performance.SDK.Processing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

namespace LinuxLogParser.WaLinuxAgentLog
{
    public class WaLinuxAgentLogParser : LogParserBase<WaLinuxAgentLogParsedEntry, LogParsedDataKey>
    {
        public override string Id => SourceParserIds.WaLinuxAgentLog;
        public override DataSourceInfo DataSourceInfo => dataSourceInfo;

        private static Regex WaLinuxAgentRegex =
            new Regex(@"^(\d{4}/\d{2}/\d{2} \d{2}:\d{2}:\d{2}\.\d{6}) (\w+) (.*)$",
                RegexOptions.Compiled);

        private DataSourceInfo dataSourceInfo;

        public WaLinuxAgentLogParser(string[] filePaths) : base(filePaths)
        {
        }

        public override void ProcessSource(
           ISourceDataProcessor<WaLinuxAgentLogParsedEntry, LogContext, LogParsedDataKey> dataProcessor,
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
                LogEntry lastLogEntry = null;

                while ((line = file.ReadLine()) != null)
                {
                    lineNumber++;

                    var WaLinuxAgentMatch = WaLinuxAgentRegex.Match(line);
                    if (!WaLinuxAgentMatch.Success)
                    {
                        // Continuation of the last line.
                        if (lastLogEntry == null)
                        {
                            throw new InvalidOperationException("Can't find the timestamp of the log.");
                        }

                        lastLogEntry.Log += '\n' + line;
                        continue;
                    }

                    // We successfully match the format of the log, which means the last log entry is completed.
                    if (lastLogEntry != null)
                    {
                        dataProcessor.ProcessDataElement(lastLogEntry, Context, cancellationToken);
                    }

                    DateTime time;
                    if (!DateTime.TryParse(WaLinuxAgentMatch.Groups[1].Value, out time))
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


                    lastLogEntry = new LogEntry(path, lineNumber, offsetEventTimestamp,
                        WaLinuxAgentMatch.Groups[2].Value, WaLinuxAgentMatch.Groups[3].Value);
                }

                if (lastLogEntry != null)
                {
                    dataProcessor.ProcessDataElement(lastLogEntry, Context, cancellationToken);
                }

                Context.UpdateFileMetadata(path, new FileMetadata(lineNumber));
            }

            var offsetEndTimestamp = new Timestamp(endTime.ToNanoseconds - startNanoSeconds);

            this.dataSourceInfo = new DataSourceInfo(0, offsetEndTimestamp.ToNanoseconds, fileStartTime);
        }
    }
}
