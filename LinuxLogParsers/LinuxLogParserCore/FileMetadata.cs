// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace LinuxLogParserCore
{
    public class FileMetadata
    {
        public ulong LineCount { get; private set; }

        public FileMetadata(ulong lineCount)
        {
            LineCount = lineCount;
        }
    }
}
