// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.Serialization;
using CtfPlayback;

namespace PerfCds.CtfExtensions
{
    /// <summary>
    /// An Perf exception related to trace playback
    /// </summary>
    public class PerfPlaybackException 
        : CtfPlaybackException
    {
        public PerfPlaybackException()
        {
        }

        public PerfPlaybackException(string message) 
            : base("Perf: " + message)
        {
        }

        public PerfPlaybackException(string message, Exception innerException) 
            : base("Perf: " + message, innerException)
        {
        }

        protected PerfPlaybackException(SerializationInfo info, StreamingContext context) 
            : base(info, context)
        {
        }
    }
}