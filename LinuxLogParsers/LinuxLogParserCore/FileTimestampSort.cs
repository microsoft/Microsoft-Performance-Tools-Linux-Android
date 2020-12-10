// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace LinuxLogParserCore
{
    public static class FileTimestampSort
    {
        public delegate bool TryExtractTimeUtcDelegate(string line, out DateTime time);

        public static SortResult FilterAndSort(string[] filePaths, TryExtractTimeUtcDelegate tryExtractTimeUtc)
        {
            int count = filePaths.Length;
            if (count == 0)
            {
                return null;
            }

            DateTime fileStartTimeUtc = DateTime.MaxValue;
            List<Tuple<string, DateTime>> fileTimeTuples = new List<Tuple<string, DateTime>>();

            for (int index = 0; index < count; index++)
            {
                string filePath = filePaths[index];
                DateTime utcTime;
                if (!tryExtractTimeUtc(filePath, out utcTime))
                {
                    // Unable to extract a timestamp in this entire file. Filter it.
                    continue;
                }
                if (utcTime < fileStartTimeUtc)
                {
                    fileStartTimeUtc = utcTime;
                }

                fileTimeTuples.Add(Tuple.Create(filePath, utcTime));
            }

            int filteredCount = fileTimeTuples.Count;

            if (filteredCount == 0)
            {
                // Unable to extract timestamp from all files.
                return null;
            }

            fileTimeTuples.Sort((tuple1, tuple2) => tuple1.Item2.CompareTo(tuple2.Item2));


            string[] filePathsSorted = new string[filteredCount];
            for (int index = 0; index < filteredCount; index++)
            {
                filePathsSorted[index] = fileTimeTuples[index].Item1;
            }

            return new SortResult(filePathsSorted, fileStartTimeUtc);
        }

        public class SortResult
        {
            public string[] FilePathsSorted { get; }
            public DateTime FileStartTimeUtc { get; }

            public SortResult(string[] filePathsSorted, DateTime fileStartTimeUtc)
            {
                FilePathsSorted = filePathsSorted;
                FileStartTimeUtc = fileStartTimeUtc;
            }
        }
    }
}
