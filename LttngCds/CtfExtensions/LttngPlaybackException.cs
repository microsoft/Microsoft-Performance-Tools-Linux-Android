// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.Serialization;
using CtfPlayback;

namespace LttngCds.CtfExtensions
{
    /// <summary>
    /// An LTTNG exception related to trace playback
    /// </summary>
    public class LttngPlaybackException 
        : CtfPlaybackException
    {
        public LttngPlaybackException()
        {
        }

        public LttngPlaybackException(string message) 
            : base("LTTNG: " + message)
        {
        }

        public LttngPlaybackException(string message, Exception innerException) 
            : base("LTTNG: " + message, innerException)
        {
        }

        protected LttngPlaybackException(SerializationInfo info, StreamingContext context) 
            : base(info, context)
        {
        }
    }
}