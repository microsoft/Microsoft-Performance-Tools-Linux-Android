// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.Serialization;
using CtfPlayback;

namespace LTTngCds.CtfExtensions
{
    /// <summary>
    /// An LTTNG exception related to trace playback
    /// </summary>
    public class LTTngPlaybackException 
        : CtfPlaybackException
    {
        public LTTngPlaybackException()
        {
        }

        public LTTngPlaybackException(string message) 
            : base("LTTNG: " + message)
        {
        }

        public LTTngPlaybackException(string message, Exception innerException) 
            : base("LTTNG: " + message, innerException)
        {
        }

        protected LTTngPlaybackException(SerializationInfo info, StreamingContext context) 
            : base(info, context)
        {
        }
    }
}