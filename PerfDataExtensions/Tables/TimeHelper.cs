// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Performance.SDK;
using System;
using System.Collections.Generic;
using System.Text;

namespace PerfDataExtensions.Tables
{
    public static class TimeHelper
    {
        public struct ReduceTimeSinceLastDiff
            : IFunc<int, Timestamp, Timestamp, TimestampDelta>
        {
            public TimestampDelta Invoke(int value, Timestamp timeSinceLast1, Timestamp timeSinceLast2)
            {
                return timeSinceLast1 - timeSinceLast2;
            }
        }

        private struct ReduceTimeMinusDelta
            : IFunc<int, Timestamp, TimestampDelta, Timestamp>
        {
            public Timestamp Invoke(int value, Timestamp timestamp, TimestampDelta delta)
            {
                return timestamp - delta;
            }
        }
    }
}
