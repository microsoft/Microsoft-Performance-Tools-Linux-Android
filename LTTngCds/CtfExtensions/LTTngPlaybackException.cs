// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.Serialization;
using CtfPlayback;

namespace LTTngCds.CtfExtensions
{
    /// <summary>
    /// An LTTng exception related to trace playback
    /// </summary>
    public class LTTngPlaybackException 
        : CtfPlaybackException
    {
        public LTTngPlaybackException()
        {
        }

        public LTTngPlaybackException(string message) 
            : base("LTTng: " + message)
        {
        }

        public LTTngPlaybackException(string message, Exception innerException) 
            : base("LTTng: " + message, innerException)
        {
        }

        protected LTTngPlaybackException(SerializationInfo info, StreamingContext context) 
            : base(info, context)
        {
        }
    }
}