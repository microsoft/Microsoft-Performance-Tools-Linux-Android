// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using LinuxLogParserCore;
using Microsoft.Performance.SDK;
using Microsoft.Performance.SDK.Extensibility.SourceParsing;
using Microsoft.Performance.SDK.Processing;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;

namespace LinuxLogParser.DmesgIsoLog
{
    public class DmesgIsoLogParser : LogParserBase<DmesgIsoLogParsedEntry, LogParsedDataKey>
    {
        public override string Id => SourceParserIds.DmesgIsoLog;
        public override DataSourceInfo DataSourceInfo => dataSourceInfo;

        private DataSourceInfo dataSourceInfo;

        public DmesgIsoLogParser(string[] filePaths) : base(filePaths)
        {
        }

        private bool IsNumberFormat(string s)
        {
            foreach (var c in s)
            {
                if (!Char.IsNumber(c) && c != '.')
                {
                    return false;
                }
            }
            return true;
        }

        private bool IsPCIinfo(string s)
        {
            return s.Length == 6 &&
                s[0] == ' ' &&
                s[1] == '[' &&
                Char.IsNumber(s[2]) &&
                Char.IsNumber(s[3]) &&
                Char.IsNumber(s[4]) &&
                Char.IsNumber(s[5]);
        }

        public override void ProcessSource(
           ISourceDataProcessor<DmesgIsoLogParsedEntry, LogContext, LogParsedDataKey> dataProcessor,
           ILogger logger, IProgress<int> progress, CancellationToken cancellationToken)
        {
            var contentDictionary = new Dictionary<string, IReadOnlyList<LogEntry>>();
            string[] lineContent;
            string[] firstSlice;
            StringBuilder builder = new StringBuilder();
            Timestamp oldestTimestamp = new Timestamp(long.MaxValue);
            Timestamp newestTImestamp = new Timestamp(long.MinValue);
            long startNanoSeconds = 0;
            DateTime fileStartTime = default;
            DateTime parsedTime = default;
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
                    //First, we check if the line is a new log entry by trying to parse its timestamp
                    if (line.Length >= 31 && DateTime.TryParseExact(line[..31], "yyyy-MM-ddTHH:mm:ss,ffffffK", dateTimeCultureInfo, DateTimeStyles.None, out parsedTime))
                    {
                        if (lastEntry != null)
                        {
                            dataProcessor.ProcessDataElement(lastEntry, Context, cancellationToken);
                        }

                        lastEntry = new LogEntry
                        {
                            filePath = path,
                            lineNumber = currentLineNumber,
                            rawLog = line[32..]
                        };

                        lineContent = lastEntry.rawLog.Split(':');

                        if (lineContent.Length == 1 || lineContent[0].Length > 20)
                        {
                            // Character ':' is not present in the beginning of the message, therefore there is no entity nor topic nor metadata
                            lastEntry.entity = string.Empty;
                            lastEntry.metadata = string.Empty;
                            lastEntry.topic = string.Empty;
                            lastEntry.message = lineContent[0];
                        }
                        else if (lineContent.Length == 2)
                        {
                            // Character ':' occurs once in the message and in the beginning, therefore there is entity but no topic nor metadata
                            lastEntry.entity = lineContent[0];
                            lastEntry.metadata = string.Empty;
                            lastEntry.topic = string.Empty;
                            lastEntry.message = lineContent[1];
                        }
                        else
                        {
                            // Character ':' occurs multiple times in the message, and at least once in the beginning
                            // We proceed to try to infer entity, metadata and topic
                            firstSlice = lineContent[0].Split(' ');
                            var lastSubstring = firstSlice[^1];
                            int contentIndex = 2;
                            if (firstSlice.Length > 1 && this.IsNumberFormat(lastSubstring) && this.IsNumberFormat(lineContent[1]))
                            {
                                //There is metadata and entity
                                builder.Clear();
                                builder.Append(lastSubstring);
                                builder.Append(':');
                                builder.Append(lineContent[1]);

                                while (contentIndex < lineContent.Length && this.IsNumberFormat(lineContent[contentIndex]))
                                {
                                    builder.Append(':');
                                    builder.Append(lineContent[contentIndex]);
                                    ++contentIndex;
                                }

                                lastEntry.metadata = builder.ToString();

                                lastEntry.entity = lineContent[0][..(lineContent[0].Length - lastSubstring.Length - 1)];

                                if ((contentIndex < lineContent.Length - 1) && !IsPCIinfo(lineContent[contentIndex]))
                                {
                                    //We check for topic after the metadata
                                    lastEntry.topic = lineContent[contentIndex];
                                    ++contentIndex;
                                }
                            }
                            else if (this.IsNumberFormat(lineContent[0]))
                            {
                                //There is metadata but no entity
                                builder.Clear();
                                builder.Append(lineContent[0]);
                                contentIndex = 1;
                                while (contentIndex < lineContent.Length && this.IsNumberFormat(lineContent[contentIndex]))
                                {
                                    builder.Append(':');
                                    builder.Append(lineContent[contentIndex]);
                                    ++contentIndex;
                                }

                                lastEntry.metadata = builder.ToString();
                                lastEntry.entity = string.Empty;
                                lastEntry.topic = string.Empty;
                            }
                            else
                            {
                                //There is entity but no metadata

                                if (lineContent[1].StartsWith(" type="))
                                {
                                    //We check for topic after the entity
                                    lastEntry.topic = string.Empty;
                                    contentIndex = 1;
                                }
                                else
                                {
                                    lastEntry.topic = lineContent[1];
                                }

                                lastEntry.entity = lineContent[0];
                                lastEntry.metadata = string.Empty;
                            }

                            builder.Clear();

                            //Remainder of the log is the message
                            if (contentIndex < lineContent.Length)
                            {
                                builder.Append(lineContent[contentIndex]);
                                ++contentIndex;
                            }

                            while (contentIndex < lineContent.Length)
                            {
                                builder.Append(':');
                                builder.Append(lineContent[contentIndex]);
                                ++contentIndex;
                            }

                            lastEntry.message = builder.ToString();
                        }

                        parsedTime = DateTime.FromFileTimeUtc(parsedTime.ToFileTimeUtc());  // Need to explicitly say log time is in UTC, otherwise it will be interpreted as local
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

                        lastEntry.timestamp = new Timestamp(timeStamp.ToNanoseconds - startNanoSeconds);

                        entriesList.Add(lastEntry);
                    }
                    else if (entriesList.Count > 0)
                    {
                        if (lastEntry == null)
                        {
                            throw new InvalidOperationException("Can't parse the log.");
                        }
                        lastEntry.message = lastEntry.message + "\n" + line;
                        lastEntry.rawLog = lastEntry.rawLog + "\n" + line;
                    }

                    ++currentLineNumber;
                }

                if (lastEntry != null)
                {
                    dataProcessor.ProcessDataElement(lastEntry, Context, cancellationToken);
                }

                contentDictionary[path] = entriesList.AsReadOnly();

                file.Close();

                --currentLineNumber;
                Context.UpdateFileMetadata(path, new FileMetadata(currentLineNumber));
            }

            var offsetEndTimestamp = new Timestamp(newestTImestamp.ToNanoseconds - startNanoSeconds);
            dataSourceInfo = new DataSourceInfo(0, offsetEndTimestamp.ToNanoseconds, fileStartTime);
        }
    }
}
