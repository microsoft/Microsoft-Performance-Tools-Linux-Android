// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace LinuxLogParserCore
{
    public class LogContext
    {
        public Dictionary<string, FileMetadata> FileToMetadata { get; } = new Dictionary<string, FileMetadata>();

        public void UpdateFileMetadata(string filePath, FileMetadata metadata)
        {
            FileToMetadata[filePath] = metadata;
        }
    }
}
