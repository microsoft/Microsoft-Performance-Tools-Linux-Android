// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using CtfPlayback.FieldValues;
using LttngCds.CookerData;
using LttngDataExtensions.DataOutputTypes;
using Microsoft.Performance.SDK;
using Microsoft.Performance.SDK.Extensibility;
using Microsoft.Performance.SDK.Extensibility.DataCooking;

namespace LttngDataExtensions.SourceDataCookers.Cpu
{

    public class LttngCpuDataCooker
        : LttngBaseSourceCooker
    {
        public static string ContextSwitchEventName = "sched_switch";
        public const string Identifier = "CpuDataCooker";

        private static readonly HashSet<string> Keys = new HashSet<string>( new[] { ContextSwitchEventName });
        readonly List<IContextSwitch> contextSwitches = new List<IContextSwitch>();

        public LttngCpuDataCooker()
            : base(Identifier)
        {
            this.DataKeys = new ReadOnlyHashSet<string>(Keys);
            this.ContextSwitches = new ProcessedEventData<IContextSwitch>();
        }

        public override string Description => "Provides information on context switching.";

        public override ReadOnlyHashSet<string> DataKeys { get; }

        public override DataProcessingResult CookDataElement(
            LttngEvent data, 
            LttngContext context, 
            CancellationToken cancellationToken)
        {
            try
            {
                ContextSwitchUserData parsed = ContextSwitchUserData.Read(data.Payload);

                ContextSwitches.AddEvent(
                    new ContextSwitch(
                        data.Timestamp,
                        parsed.PrevComm,
                        parsed.PrevTid,
                        parsed.PrevPrio,
                        parsed.NextComm,
                        parsed.NextTid,
                        parsed.NextPrio));
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
                return DataProcessingResult.CorruptData;
            }

            return DataProcessingResult.Processed;
        }

        public override void EndDataCooking(CancellationToken cancellationToken)
        {
            this.ContextSwitches.FinalizeData();
        }

        [DataOutput]
        public ProcessedEventData<IContextSwitch> ContextSwitches { get; }

        struct ContextSwitchUserData
        {
            public string PrevComm;
            public int PrevTid;
            public int PrevPrio;
            public long PrevState;
            public string NextComm;
            public int NextTid;
            public int NextPrio;

            public static ContextSwitchUserData Read(CtfStructValue data)
            {
                return new ContextSwitchUserData
                {
                    PrevComm = data.ReadFieldAsArray("_prev_comm").ReadAsString(),
                    PrevTid = data.ReadFieldAsInt32("_prev_tid"),
                    PrevPrio = data.ReadFieldAsInt32("_prev_prio"),
                    PrevState = data.ReadFieldAsInt64("_prev_state"),
                    NextComm = data.ReadFieldAsArray("_next_comm").ReadAsString(),
                    NextTid = data.ReadFieldAsInt32("_next_tid"),
                    NextPrio = data.ReadFieldAsInt32("_next_prio")
                };
            }
        }
    }
}