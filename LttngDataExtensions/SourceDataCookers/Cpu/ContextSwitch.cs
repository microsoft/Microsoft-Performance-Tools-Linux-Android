// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using LttngDataExtensions.DataOutputTypes;
using Microsoft.Performance.SDK;

namespace LttngDataExtensions.SourceDataCookers.Cpu
{
    public struct ContextSwitch 
        : IContextSwitch, 
          IContextSwitchIn, 
          IContextSwitchOut
    {
        readonly Timestamp timestamp;
        readonly string switchInImageName;
        readonly int switchInThreadId;
        readonly int switchInThreadPriority;
        readonly string switchOutImageName;
        readonly int switchOutThreadId;
        readonly int switchOutThreadPriority;

        public ContextSwitch(Timestamp timestamp, string switchInImageName, int switchInThreadId,
            int switchInThreadPriority, string switchOutImageName, int switchOutThreadId,
            int switchOutThreadPriority)
        {
            this.timestamp = timestamp;
            this.switchInImageName = switchInImageName;
            this.switchInThreadId = switchInThreadId;
            this.switchInThreadPriority = switchInThreadPriority;
            this.switchOutImageName = switchOutImageName;
            this.switchOutThreadId = switchOutThreadId;
            this.switchOutThreadPriority = switchOutThreadPriority;
        }

        public Timestamp Timestamp => timestamp;

        public IContextSwitchIn SwitchIn => this;

        public IContextSwitchOut SwitchOut => this;

        string IContextSwitchIn.ImageName => switchInImageName;

        int IContextSwitchIn.ThreadId => switchInThreadId;

        int IContextSwitchIn.Priority => switchInThreadPriority;

        string IContextSwitchOut.ImageName => switchOutImageName;

        int IContextSwitchOut.ThreadId => switchOutThreadId;

        int IContextSwitchOut.Priority => switchOutThreadPriority;
    }
}
