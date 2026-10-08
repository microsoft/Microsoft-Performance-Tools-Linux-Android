// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace LTTngCds.CtfExtensions
{
    internal static class LTTngStreamFiles
    {
        /// <summary>
        /// LTTng names event stream files "&lt;channel name&gt;_&lt;cpu&gt;", e.g. "channel0_3" or "sw-chan_3".
        /// </summary>
        /// <param name="fileName">File name without directory</param>
        /// <returns>true if the file is an event stream</returns>
        internal static bool IsEventStreamFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName) ||
                fileName.StartsWith(".", StringComparison.Ordinal) ||
                StringComparer.Ordinal.Equals(fileName, "metadata"))
            {
                return false;
            }

            int separatorIndex = fileName.LastIndexOf('_');
            if (separatorIndex <= 0 || separatorIndex == fileName.Length - 1)
            {
                return false;
            }

            for (int index = separatorIndex + 1; index < fileName.Length; index++)
            {
                if (!char.IsDigit(fileName[index]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
