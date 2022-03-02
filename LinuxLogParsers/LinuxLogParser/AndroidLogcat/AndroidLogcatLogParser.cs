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
using System.IO;
using System.Linq;
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
        public static Regex AndroidLogCatRegex = new Regex(@"^([0-9-]+ [0-9:.]+)\s+(\d+)\s+(\d+)\s+(\S) (.+?)\s?: (.*)$");

        const long SECONDS_TO_NS = 1000000000;
        const long MS_TO_NS = 1000000;
        const long US_TO_NS = 1000;
        static readonly TimestampDelta OneNanoSecondTimestampDelta = new TimestampDelta(1);  // At least 1ns timestamp for duration

        public AndroidLogcatLogParser(string[] filePaths) : base(filePaths)
        {
        }

        public override void ProcessSource(
           ISourceDataProcessor<AndroidLogcatLogParsedEntry, LogContext, LogParsedDataKey> dataProcessor,
           ILogger logger, IProgress<int> progress, CancellationToken cancellationToken)
        {
            var contentDictionary = new Dictionary<string, IReadOnlyList<LogEntry>>();
            Timestamp oldestTimestamp = new Timestamp(long.MaxValue);
            Timestamp newestTimestamp = new Timestamp(long.MinValue);
            long startNanoSeconds = 0;
            DateTime fileStartTime = default;
            var dateTimeCultureInfo = new CultureInfo("en-US");
            double? utcOffsetInHours = null;
            var processingExceptions = new List<Exception>();

            foreach (var path in FilePaths)
            {
                ulong currentLineNumber = 1;
                string line = string.Empty;

                try
                {
                    var file = new System.IO.StreamReader(path);
                    var logEntries = new List<LogEntry>();
                    var durationLogEntries = new List<DurationLogEntry>();

                    string timeDateFormat = null;

                    var rootFolder = Path.GetDirectoryName(path);
                    var utcOffsetFilePath = Path.Combine(rootFolder, "utcoffset.txt");
                    if (File.Exists(utcOffsetFilePath))
                    {
                        var utcOffSetStr = File.ReadAllText(utcOffsetFilePath);
                        if (double.TryParse(utcOffSetStr, out double utcOffsetTmp))
                        {
                            utcOffsetInHours = utcOffsetTmp;
                        }
                    }

                    while ((line = file.ReadLine()) != null)
                    {
                        LogEntry logEntry = null;
                        // Optimization - don't save blank lines
                        if (line == String.Empty)
                        {
                            currentLineNumber++;
                            continue;
                        }

                        var androidLogCatMatch = AndroidLogCatRegex.Match(line);

                        if (androidLogCatMatch.Success && timeDateFormat == null)
                        {
                            const string monthDayFormat = "MM-dd HH:mm:ss.fff";
                            const string monthDayYearFormat = "MM-dd-yyyy HH:mm:ss.fff";
                            if (DateTime.TryParseExact(androidLogCatMatch.Groups[1].Value, monthDayFormat, dateTimeCultureInfo, DateTimeStyles.None, out _))
                            {
                                timeDateFormat = monthDayFormat;
                            }
                            else if (DateTime.TryParseExact(androidLogCatMatch.Groups[1].Value, monthDayYearFormat, dateTimeCultureInfo, DateTimeStyles.None, out _))
                            {
                                timeDateFormat = monthDayYearFormat;
                            }
                            else
                            {
                                throw new Exception($"Invalid date/time format: {androidLogCatMatch.Groups[1].Value}");
                            }
                        }

                        // First, we check if the line is a new log entry if it matched Regex and by trying to parse its timestamp
                        if (androidLogCatMatch.Success &&
                            androidLogCatMatch.Groups.Count >= 7 &&
                            DateTime.TryParseExact(androidLogCatMatch.Groups[1].Value, timeDateFormat, dateTimeCultureInfo, DateTimeStyles.None, out DateTime parsedTime))
                        {
                            var timeStamp = Timestamp.FromNanoseconds(parsedTime.Ticks * 100);

                            if (timeStamp < oldestTimestamp)
                            {
                                oldestTimestamp = timeStamp;
                                fileStartTime = parsedTime;
                                startNanoSeconds = oldestTimestamp.ToNanoseconds;
                            }
                            if (timeStamp > newestTimestamp)
                            {
                                newestTimestamp = timeStamp;
                            }

                            logEntry = new LogEntry
                            {
                                Timestamp = new Timestamp(timeStamp.ToNanoseconds), // We can't subtract startNanoSeconds here because logs are not necessarily time ordered
                                FilePath = path,
                                LineNumber = currentLineNumber,
                                PID = uint.Parse(androidLogCatMatch.Groups[2].Value),
                                TID = uint.Parse(androidLogCatMatch.Groups[3].Value),
                                Priority = Utilities.Common.StringIntern(androidLogCatMatch.Groups[4].Value),
                                Tag = Utilities.Common.StringIntern(androidLogCatMatch.Groups[5].Value.Trim()),
                                Message = androidLogCatMatch.Groups[6].Value,
                            };

                            // Specialized Duration parsing
                            DurationLogEntry durationLogEntry = null;
                            if (logEntry.Tag == "init" && logEntry.Message.Contains("took"))
                            {
                                var messageSplit = logEntry.Message.Split();
                                var firstSingleQuoteIdx = logEntry.Message.IndexOf('\'');
                                var secondSingleQuoteIdx = logEntry.Message.IndexOf('\'', firstSingleQuoteIdx + 1);
                                var name = logEntry.Message.Substring(firstSingleQuoteIdx + 1, secondSingleQuoteIdx - firstSingleQuoteIdx - 1);
                                // Command 'write /dev/cpuctl/cpu.rt_period_us 1000000' action=init (/system/etc/init/hw/init.rc:271) took 0ms and failed: ....
                                if (logEntry.Message.Contains("0ms"))
                                {
                                    durationLogEntry = LogEntryFromDurationNs(0, logEntry, name);
                                }
                                // Service 'boringssl_self_test32_vendor' (pid 18) exited with status 0 waiting took 0.022000 seconds

                                if (messageSplit[^3] == "took" && messageSplit[^1] == "seconds")
                                {
                                    if (double.TryParse(messageSplit[^2], out double durationSeconds))
                                    {
                                        durationLogEntry = LogEntryFromDurationS(durationSeconds, logEntry, name);
                                    }
                                }
                                // Command 'mount_all /fstab.${ro.hardware}' action=fs (/vendor/etc/init/init.windows_x86_64.rc:14) took 1054ms and succeeded
                                else if (messageSplit[^4] == "took" && messageSplit[^1] == "succeeded")
                                {
                                    if (int.TryParse(messageSplit[^3].Replace("ms", String.Empty), out int durationMs))
                                    {
                                        durationLogEntry = LogEntryFromDurationMs(durationMs, logEntry, name);
                                    }
                                }
                                // Service 'disable_lro' (pid 39) exited with status 0 oneshot service took 0.006000 seconds in background
                                else if (messageSplit[^5] == "took" && messageSplit[^1] == "background")
                                {
                                    if (double.TryParse(messageSplit[^4], out double durationSeconds))
                                    {
                                        durationLogEntry = LogEntryFromDurationS(durationSeconds, logEntry, name);
                                    }
                                }
                                // Command 'rm /data/user/0' action=post-fs-data (/system/etc/init/hw/init.rc:706) took 1ms and failed: unlink() failed: Is a directory
                                else
                                {
                                    var tookIndex = Array.IndexOf(messageSplit, "took");
                                    var afterTook = messageSplit[tookIndex + 1];
                                    if (afterTook.Contains("ms"))
                                    {
                                        if (int.TryParse(afterTook.Replace("ms", String.Empty), out int durationMs))
                                        {
                                            durationLogEntry = LogEntryFromDurationMs(durationMs, logEntry, name);
                                        }
                                    }
                                }
                            }
                            // Zygote32Timing: PostZygoteInitGC took to complete: 61ms
                            else if (logEntry.Tag.Contains("Timing") && logEntry.Message.Contains("took to complete"))
                            {
                                var messageSplit = logEntry.Message.Split();
                                var name = messageSplit[0];
                                if (int.TryParse(messageSplit[^1].Replace("ms", String.Empty), out int durationMs))
                                {
                                    durationLogEntry = LogEntryFromDurationMs(durationMs, logEntry, name);
                                }
                            }
                            else if (logEntry.Tag == "SurfaceFlinger" && logEntry.Message.Contains("Boot is finished"))
                            {
                                var messageSplit = logEntry.Message.Split();
                                if (int.TryParse(messageSplit[^2].Replace("(", String.Empty), out int durationMs))
                                {
                                    durationLogEntry = LogEntryFromDurationMs(durationMs, logEntry, "Boot is finished");
                                }
                            }
                            else if (logEntry.Tag == "Looper" && logEntry.Message.StartsWith("Slow"))
                            {
                                var messageSplit = logEntry.Message.Split();
                                if (messageSplit.Length >= 6 && int.TryParse(messageSplit[3].Replace("ms", String.Empty), out int durationMs))
                                {
                                    durationLogEntry = LogEntryFromDurationMs(durationMs, logEntry, $"{messageSplit[4]} {messageSplit[5]}");
                                }
                            }
                            else if (logEntry.Tag == "OpenGLRenderer" && logEntry.Message.StartsWith("Davey!")) // Jank & Perf
                            {
                                var messageSplit = logEntry.Message.Split();
                                if (messageSplit.Length >= 2 && int.TryParse(messageSplit[1].Replace("duration=", String.Empty).Replace("ms;", String.Empty), out int durationMs))
                                {
                                    durationLogEntry = LogEntryFromDurationMs(durationMs, logEntry, "OpenGLRenderer Jank Perf");
                                }
                            }
                            else if (logEntry.Tag == "Zygote" && logEntry.Message.EndsWith("ms."))
                            {
                                var messageSplit = logEntry.Message.Split();
                                if (int.TryParse(messageSplit[^1].Replace("ms.", String.Empty), out int durationMs))
                                {
                                    durationLogEntry = LogEntryFromDurationMs(durationMs, logEntry, logEntry.Tag);
                                }
                            }
                            // "Creating child chains: 13886us" - UserDebug
                            else if (logEntry.Tag == "netd" && logEntry.Message.EndsWith("us"))
                            {
                                var messageSplit = logEntry.Message.Split();
                                if (int.TryParse(messageSplit[^1].Replace("us", String.Empty), out int durationUs))
                                {
                                    durationLogEntry = LogEntryFromDurationUs(durationUs, logEntry, logEntry.Tag);
                                }
                            }
                            // "RenderEngine: shader cache generated - 48 shaders in 1452.951172 ms" - UserDebug
                            else if (logEntry.Tag == "RenderEngine" && logEntry.Message.StartsWith("shader cache generated") && logEntry.Message.EndsWith(" ms"))
                            {
                                var messageSplit = logEntry.Message.Split();
                                if (messageSplit.Length >= 9 && double.TryParse(messageSplit[7], out double durationMs))
                                {
                                    durationLogEntry = LogEntryFromDurationMs(durationMs, logEntry, logEntry.Tag);
                                }
                            }
                            // Android 12
                            else if (logEntry.Tag == "ServiceManager" && logEntry.Message.Contains("successful after waiting"))
                            {
                                var messageSplit = logEntry.Message.Split();
                                if (messageSplit.Length >= 3 && int.TryParse(messageSplit[^1].Replace("ms", String.Empty), out int durationMs))
                                {
                                    durationLogEntry = LogEntryFromDurationMs(durationMs, logEntry, messageSplit[3].Replace("'", String.Empty));
                                }
                            }
                            else if (logEntry.Tag == "PackageManager" && logEntry.Message.StartsWith("Finished scanning"))
                            {
                                var messageSplit = logEntry.Message.Split();
                                if (messageSplit.Length >= 6 && int.TryParse(messageSplit[5], out int durationMs))
                                {
                                    durationLogEntry = LogEntryFromDurationMs(durationMs, logEntry, messageSplit[2] + messageSplit[3]);
                                }
                            }
                            else if (logEntry.Tag == "dex2oat32" && logEntry.Message.StartsWith("dex2oat took"))
                            {
                                var messageSplit = logEntry.Message.Split();
                                if (messageSplit[2].EndsWith("ms"))
                                {
                                    if (messageSplit.Length >= 3 && double.TryParse(messageSplit[2].Replace("ms", String.Empty), out double durationMs))
                                    {
                                        durationLogEntry = LogEntryFromDurationMs(durationMs, logEntry, logEntry.Tag);
                                    }
                                }
                                else
                                {
                                    if (messageSplit.Length >= 3 && double.TryParse(messageSplit[2].Replace("s", String.Empty), out double durationS))
                                    {
                                        durationLogEntry = LogEntryFromDurationS(durationS, logEntry, logEntry.Tag);
                                    }
                                }
                            }

                            if (durationLogEntry != null)
                            {
                                durationLogEntries.Add(durationLogEntry);
                            }
                        }
                        else
                        {
                            logEntry = new LogEntry
                            {
                                Timestamp = Timestamp.MinValue,
                                FilePath = path,
                                LineNumber = currentLineNumber,
                                Message = line,
                            };
                        }

                        if (logEntry != null)
                        {
                            logEntries.Add(logEntry);
                        }

                        currentLineNumber++;
                    }

                    // Synthetic durations

                    // Kernel Boot
                    try
                    {
                        var kernelStart = logEntries.SingleOrDefault(f => f.LineNumber <= 100 && f.Tag == String.Empty && f.Message.StartsWith("Linux version"));
                        var initStart = logEntries.SingleOrDefault(f => f.Tag == "init" && f.Message == "init first stage started!");
                        if (kernelStart != null && initStart != null)
                        {
                            const string kernelBoot = "Kernel Boot";
                            var kernelBootLogEntry = new DurationLogEntry()
                            {
                                StartTimestamp = kernelStart.Timestamp,
                                EndTimestamp = initStart.Timestamp,
                                Duration = initStart.Timestamp - kernelStart.Timestamp,
                                FilePath = initStart.FilePath,
                                LineNumber = initStart.LineNumber,
                                PID = initStart.PID,
                                TID = initStart.TID,
                                Priority = initStart.Priority,
                                Message = kernelBoot,
                                Name = kernelBoot,
                            };
                            durationLogEntries.Add(kernelBootLogEntry);
                        }
                    }
                    catch (InvalidOperationException)
                    {
                        logger.Error("Unable to process kernel boot region - multiple \"Linux version\" start and \"init first stage started!\" end detected");
                    }

                    // Now adjust times to start of earliest message given out of order timestamps
                    foreach (var le in logEntries)
                    {
                        if (le.Timestamp != Timestamp.MinValue)
                        {
                            le.Timestamp = Timestamp.FromNanoseconds(le.Timestamp.ToNanoseconds - startNanoSeconds);
                        }
                        dataProcessor.ProcessDataElement(le, Context, cancellationToken);
                    }
                    foreach (var durationLogEntry in durationLogEntries)
                    {
                        durationLogEntry.StartTimestamp = Timestamp.FromNanoseconds(durationLogEntry.StartTimestamp.ToNanoseconds - startNanoSeconds);
                        durationLogEntry.EndTimestamp = Timestamp.FromNanoseconds(durationLogEntry.EndTimestamp.ToNanoseconds - startNanoSeconds);

                        dataProcessor.ProcessDataElement(durationLogEntry, Context, cancellationToken);
                    }

                    contentDictionary[path] = logEntries.AsReadOnly();

                    file.Close();

                    --currentLineNumber;
                    Context.UpdateFileMetadata(path, new FileMetadata(currentLineNumber));
                }
                catch (Exception ex)
                {
                    logger.Fatal($"Error processing line number {currentLineNumber} \"{line}\" on {path}. {ex.Message}");
                    logger.Fatal($"{ex}");
                    processingExceptions.Add(ex);
                }
            }

            var offsetEndTimestamp = new Timestamp(newestTimestamp.ToNanoseconds - startNanoSeconds);

            if (utcOffsetInHours.HasValue)
            {
                // Log is in local time (not UTC) but we can use utcOffset.txt hint file to modify
                var fileStartTimeUtc = fileStartTime.Subtract(new TimeSpan(0, (int) (utcOffsetInHours.Value * 60), 0));
                dataSourceInfo = new DataSourceInfo(0, offsetEndTimestamp.ToNanoseconds, DateTime.FromFileTimeUtc(fileStartTimeUtc.ToFileTimeUtc()));
            }
            else
            {
                dataSourceInfo = new DataSourceInfo(0, offsetEndTimestamp.ToNanoseconds, DateTime.FromFileTimeUtc(fileStartTime.ToFileTime())); // Treat as current locale local time (default)
            }

            if (processingExceptions.Any())
            {
                throw new AggregateException(processingExceptions);
            }
        }

        private DurationLogEntry LogEntryFromDurationS(double durationS, LogEntry logEntry, string name)
        {
            return LogEntryFromDurationNs((long) (durationS * SECONDS_TO_NS), logEntry, name);
        }


        private DurationLogEntry LogEntryFromDurationMs(int durationMs, LogEntry logEntry, string name)
        {
            return LogEntryFromDurationNs(durationMs * MS_TO_NS, logEntry, name);
        }

        private DurationLogEntry LogEntryFromDurationMs(double durationMs, LogEntry logEntry, string name)
        {
            return LogEntryFromDurationNs((long) (durationMs * MS_TO_NS), logEntry, name);
        }

        private DurationLogEntry LogEntryFromDurationUs(int durationUs, LogEntry logEntry, string name)
        {
            return LogEntryFromDurationNs(durationUs * US_TO_NS, logEntry, name);
        }

        private DurationLogEntry LogEntryFromDurationNs(long durationNs, LogEntry logEntry, string name)
        {
            if (durationNs == 0)
            {
                return new DurationLogEntry(logEntry, logEntry.Timestamp - OneNanoSecondTimestampDelta, OneNanoSecondTimestampDelta, name);
            }
            else
            {
                var duration = new TimestampDelta(durationNs);
                return new DurationLogEntry(logEntry, logEntry.Timestamp - duration, duration, name);
            }
        }
    }
}
