// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;

namespace CtfPlayback.Helpers
{
    internal static class StreamExts
    {
        /// <summary>
        /// Reads a sequence of bytes from the current stream and advances the position within the stream by the number of bytes read.
        /// Keeps behavior of pre .NET 6.0 code which reads until count is reached or EOF
        /// .NET 6.0 breaking change - https://learn.microsoft.com/en-us/dotnet/core/compatibility/core-libraries/6.0/partial-byte-reads-in-streams - bytes read can be less than what was requested
        /// </summary>
        /// <param name="stream">The stream to read from</param>
        /// <param name="buffer">
        /// An array of bytes. When this method returns, the buffer contains the specified
        ///     byte array with the values between offset and (offset + count - 1) replaced by
        ///     the bytes read from the current source.
        /// </param>
        /// <param name="offset">The zero-based byte offset in buffer at which to begin storing the data read from the current stream.</param>
        /// <param name="count">The number of bytes to be read from the current stream.</param>
        /// <returns>The total number of bytes read into the buffer. This can be zero (0) if the end of the stream has been reached.</returns>
        public static int ReadUntilBytesRequested(this Stream stream, byte[] buffer, int offset, int count)
        {
            int read = stream.Read(buffer, offset, count);
            if (read == 0)
            {
                return 0;
            }

            if (read != count) // .NET 6.0 breaking change - https://learn.microsoft.com/en-us/dotnet/core/compatibility/core-libraries/6.0/partial-byte-reads-in-streams - bytes read can be less than what was requested
            {
                while (read < buffer.Length) // Keep reading until we fill up our buffer to the count
                {
                    int tmpBytesRead = stream.Read(buffer.AsSpan().Slice(read));
                    if (tmpBytesRead == 0) break;
                    read += tmpBytesRead;
                }
            }

            return read;
        }
    }
}