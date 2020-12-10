// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.Serialization;

namespace CtfPlayback
{
    /// <summary>
    /// This exception is thrown when the unexpected happens while playing back the CTF event streams.
    /// </summary>
    public class CtfPlaybackException
        : Exception
    {
        public CtfPlaybackException()
        {
        }

        public CtfPlaybackException(string message) : base(message)
        {
        }

        public CtfPlaybackException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected CtfPlaybackException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}