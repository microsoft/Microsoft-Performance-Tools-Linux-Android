// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using LinuxLogParserCore;
using Microsoft.Performance.SDK;
using Microsoft.Performance.SDK.Extensibility.SourceParsing;
using Microsoft.Performance.SDK.Processing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace LinuxLogParser.AndroidLogcat
{
    public class AndroidLogcatLogParser : LogParserBase<AndroidLogcatLogParsedEntry, LogParsedDataKey>
    {
        public override string Id => SourceParserIds.AndroidLogcatLog;
        public override DataSourceInfo DataSourceInfo => dataSourceInfo;

        private DataSourceInfo dataSourceInfo;

        ///  Per https://developer.android.com/studio/debug/am-logcat
        ///  date time PID    TID    priority tag: message
        ///  Example: "12-13 10:32:24.869    26    26 I Checkpoint: cp_prepareCheckpoint called"
        public static Regex AndroidLogCatRegex = new Regex(@"^(.{18})\s+(\d+)\s+(\d+)\s+(\S) (.+?)\s?: (.*)$");

        public AndroidLogcatLogParser(string[] filePaths) : base(filePaths)
        {
        }

        public override void ProcessSource(
           ISourceDataProcessor<AndroidLogcatLogParsedEntry, LogContext, LogParsedDataKey> dataProcessor,
           ILogger logger, IProgress<int> progress, CancellationToken cancellationToken)
        {
            var contentDictionary = new Dictionary<string, IReadOnlyList<LogEntry>>();
            Timestamp oldestTimestamp = new Timestamp(long.MaxValue);
            Timestamp newestTImestamp = new Timestamp(long.MinValue);
            long startNanoSeconds = 0;
            DateTime fileStartTime = default;
            var dateTimeCultureInfo = new CultureInfo("en-US");

            foreach (var path in FilePaths)
            {
                ulong currentLineNumber = 1;
                var file = new System.IO.StreamReader(path);
                string line = string.Empty;
                var entriesList = new List<LogEntry>();
                LogEntry lastEntry = null;

                while ((line = file.ReadLine()) != null)
                {
                    // Optimization - don't save blank lines
                    if (line == String.Empty)
                    {
                        currentLineNumber++;
                        continue;
                    }

                    var androidLogCatMatch = AndroidLogCatRegex.Match(line);

                    // First, we check if the line is a new log entry if it matched Regex and by trying to parse its timestamp
                    if (androidLogCatMatch.Success &&
                        androidLogCatMatch.Groups.Count >= 7 &&
                        DateTime.TryParseExact(androidLogCatMatch.Groups[1].Value, "MM-dd HH:mm:ss.fff", dateTimeCultureInfo, DateTimeStyles.None, out DateTime parsedTime))
                    {
                        var timeStamp = Timestamp.FromNanoseconds(parsedTime.Ticks * 100);

                        if (timeStamp < oldestTimestamp)
                        {
                            oldestTimestamp = timeStamp;
                            fileStartTime = parsedTime;
                            startNanoSeconds = oldestTimestamp.ToNanoseconds;
                        }
                        if (timeStamp > newestTImestamp)
                        {
                            newestTImestamp = timeStamp;
                        }

                        lastEntry = new LogEntry
                        {
                            Timestamp = new Timestamp(timeStamp.ToNanoseconds - startNanoSeconds),
                            FilePath = path,
                            LineNumber = currentLineNumber,
                            PID = uint.Parse(androidLogCatMatch.Groups[2].Value),
                            TID = uint.Parse(androidLogCatMatch.Groups[3].Value),
                            Priority = androidLogCatMatch.Groups[4].Value,
                            Tag = androidLogCatMatch.Groups[5].Value.Trim(),
                            Message = androidLogCatMatch.Groups[6].Value,
                        };

                        entriesList.Add(lastEntry);
                    }
                    else
                    {
                        lastEntry = new LogEntry
                        {
                            Timestamp = Timestamp.MinValue,
                            FilePath = path,
                            LineNumber = currentLineNumber,
                            Message = line,
                        };

                        entriesList.Add(lastEntry);
                    }

                    if (lastEntry != null)
                    {
                        dataProcessor.ProcessDataElement(lastEntry, Context, cancellationToken);
                    }

                    currentLineNumber++;
                }

                contentDictionary[path] = entriesList.AsReadOnly();

                file.Close();

                --currentLineNumber;
                Context.UpdateFileMetadata(path, new FileMetadata(currentLineNumber));
            }

            var offsetEndTimestamp = new Timestamp(newestTImestamp.ToNanoseconds - startNanoSeconds);
            dataSourceInfo = new DataSourceInfo(0, offsetEndTimestamp.ToNanoseconds, DateTime.FromFileTimeUtc(fileStartTime.ToFileTime()));
        }
    }
}
