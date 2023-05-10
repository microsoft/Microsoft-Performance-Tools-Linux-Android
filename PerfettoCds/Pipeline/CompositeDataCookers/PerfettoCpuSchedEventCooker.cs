// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Performance.SDK;
using Microsoft.Performance.SDK.Extensibility;
using Microsoft.Performance.SDK.Extensibility.DataCooking;
using Microsoft.Performance.SDK.Processing;
using PerfettoCds.Pipeline.DataOutput;
using PerfettoCds.Pipeline.SourceDataCookers;
using PerfettoProcessor;

namespace PerfettoCds.Pipeline.CompositeDataCookers
{
    /// <summary>
    /// Pulls data from multiple individual SQL tables and joins them to create a CPU scheduling event
    /// </summary>
    public sealed class PerfettoCpuSchedEventCooker : CookedDataReflector, ICompositeDataCookerDescriptor
    {
        public static readonly DataCookerPath DataCookerPath = PerfettoPluginConstants.CpuSchedEventCookerPath;

        public string Description => "CPU scheduling event composite cooker";

        public DataCookerPath Path => DataCookerPath;

        // Declare all of the cookers that are used by this CompositeCooker.
        public IReadOnlyCollection<DataCookerPath> RequiredDataCookers => new[]
        {
            PerfettoPluginConstants.ThreadCookerPath,
            PerfettoPluginConstants.ProcessRawCookerPath,
            PerfettoPluginConstants.SchedSliceCookerPath,
            PerfettoPluginConstants.FtraceEventCookerPath
        };

        [DataOutput]
        public ProcessedEventData<PerfettoCpuSchedEvent> CpuSchedEvents { get; }

        [DataOutput]
        public ProcessedEventData<PerfettoCpuWakeEvent> CpuWakeEvents { get; }

        public PerfettoCpuSchedEventCooker() : base(PerfettoPluginConstants.CpuSchedEventCookerPath)
        {
            this.CpuSchedEvents = new ProcessedEventData<PerfettoCpuSchedEvent>();
            this.CpuWakeEvents = new ProcessedEventData<PerfettoCpuWakeEvent>();
        }

        public void OnDataAvailable(IDataExtensionRetrieval requiredData)
        {
            // Gather the data from all the SQL tables
            var threadData = requiredData.QueryOutput<ProcessedEventData<PerfettoThreadEvent>>(new DataOutputPath(PerfettoPluginConstants.ThreadCookerPath, nameof(PerfettoThreadCooker.ThreadEvents)));
            var processData = requiredData.QueryOutput<ProcessedEventData<PerfettoProcessRawEvent>>(new DataOutputPath(PerfettoPluginConstants.ProcessRawCookerPath, nameof(PerfettoProcessRawCooker.ProcessEvents)));
            PopulateCpuSchedulingEvents(requiredData, threadData, processData);
        }

        void PopulateCpuSchedulingEvents(IDataExtensionRetrieval requiredData, ProcessedEventData<PerfettoThreadEvent> threadData, ProcessedEventData<PerfettoProcessRawEvent> processData)
        {
            var schedSliceData = requiredData.QueryOutput<ProcessedEventData<PerfettoSchedSliceEvent>>(new DataOutputPath(PerfettoPluginConstants.SchedSliceCookerPath, nameof(PerfettoSchedSliceCooker.SchedSliceEvents)));

            // The sched slice data contains the timings, CPU, priority, and end state info. We get the process and thread from
            // those respective tables
            var joined = from schedSlice in schedSliceData
                         join thread in threadData on schedSlice.Utid equals thread.Utid
                         join threadProcess in processData on thread.Upid equals threadProcess.Upid into pd
                         from threadProcess in pd.DefaultIfEmpty()
                         select new { schedSlice, thread, threadProcess };

            // Populate CPU wake events to find associated wake events.
            PopulateCpuWakeEvents(requiredData, threadData, processData);
            Dictionary<long, List<PerfettoCpuWakeEvent>> wokenTidToWakeEventsMap = this.CpuWakeEvents
                .GroupBy(w => w.WokenTid)
                .ToDictionary(wg => wg.Key, wg => wg.OrderBy(w => w.Timestamp).ToList());

            // Create events out of the joined results
            foreach (var result in joined)
            {
                // An event can have a thread+process or just a process
                string processName = string.Empty;
                string threadName = $"{result.thread.Name} ({result.thread.Tid})";
                if (result.threadProcess != null)
                {
                    processName = $"{result.threadProcess.Name} ({result.threadProcess.Pid})";
                }

                // Find associated CPU wake event.
                long tid = result.thread.Tid;
                Timestamp startTimestamp = new Timestamp(result.schedSlice.RelativeTimestamp);
                Timestamp endTimestamp = new Timestamp(result.schedSlice.RelativeTimestamp + result.schedSlice.Duration);

                PerfettoCpuSchedEvent ev = new PerfettoCpuSchedEvent
                (
                    processName,
                    threadName,
                    tid,
                    new TimestampDelta(result.schedSlice.Duration),
                    startTimestamp,
                    endTimestamp,
                    result.schedSlice.Cpu,
                    result.schedSlice.EndStateStr,
                    result.schedSlice.Priority
                );

                this.CpuSchedEvents.AddEvent(ev);
            }

            this.CpuSchedEvents.FinalizeData();

            // Add previous scheduling event info.
            // This needs to be done after FinalizeData call to make sure enumeration and indexing is available.
            var tidToSwitchEventsMap = this.CpuSchedEvents
                .Where(s => s != null)
                .GroupBy(s => s.Tid)
                .ToDictionary(sg => sg.Key, sg => sg.OrderBy(s => s.StartTimestamp).ToList());

            foreach (var tid in tidToSwitchEventsMap.Keys)
            {
                var cpuSchedEventsForCurrentThread = tidToSwitchEventsMap[tid];

                for (int i = 1; i < cpuSchedEventsForCurrentThread.Count; i++)
                {
                    cpuSchedEventsForCurrentThread[i].AddPreviousCpuSchedulingEvent(cpuSchedEventsForCurrentThread[i - 1]);
                }
            }

            // Add wake event info if required.
            foreach (var schedEvent in this.CpuSchedEvents)
            {
                // If the thread state was already runnable then there will be no corresponding wake event.
                if (schedEvent.PreviousSchedulingEvent?.EndState == "Runnable")
                {
                    continue;
                }

                if (wokenTidToWakeEventsMap.TryGetValue(schedEvent.Tid, out List<PerfettoCpuWakeEvent> wakeEvents))
                {
                    schedEvent.AddWakeEvent(GetWakeEvent(wakeEvents, schedEvent.StartTimestamp));
                }
            }
        }

        void PopulateCpuWakeEvents(IDataExtensionRetrieval requiredData, ProcessedEventData<PerfettoThreadEvent> threadData, ProcessedEventData<PerfettoProcessRawEvent> processData)
        {
            var schedWakeData = requiredData.QueryOutput<ProcessedEventData<PerfettoFtraceEvent>>(new DataOutputPath(PerfettoPluginConstants.FtraceEventCookerPath, nameof(PerfettoFtraceEventCooker.FtraceEvents)))
                .Where(f => f.Name == "sched_wakeup");

            Dictionary<uint, PerfettoThreadEvent> tidToThreadMap = threadData
                .ToLookup(t => t.Tid)
                .ToDictionary(tg => tg.Key, tg => tg.Last());
            Dictionary<uint, PerfettoProcessRawEvent> upidToProcessMap = processData
                .ToLookup(p => p.Upid)
                .ToDictionary(pg => pg.Key, pg => pg.Last());

            // Create events out of the joined results
            foreach (var wake in schedWakeData)
            {
                var wokenTid = uint.Parse(wake.Args.ElementAt(1).Value.ToString()); // This field name is pid but it is woken thread's Tid.
                PerfettoThreadEvent wokenThread = tidToThreadMap[wokenTid];
                string wokenThreadName = wokenThread.Name;
                var wokenPid = wokenThread.Upid;
                object comm;
                wake.Args.TryGetValue("comm", out comm);  // This field name is comm but it is woken process name.
                string wokenProcessName = wokenPid != null ? upidToProcessMap[wokenPid.Value].Name : (string)comm;

                string wakerThreadName = wake.ThreadName;
                var wakerTid = wake.Tid;
                PerfettoThreadEvent wakerThread = tidToThreadMap[wakerTid];
                var wakerPid = wakerThread.Upid;
                string wakerProcessName = wakerPid != null ? upidToProcessMap[wakerPid.Value].Name : String.Empty;

                object prio;
                wake.Args.TryGetValue("prio", out prio);
                object success;
                wake.Args.TryGetValue("success", out success);
                object target_cpu;
                wake.Args.TryGetValue("target_cpu", out target_cpu);
                PerfettoCpuWakeEvent ev = new PerfettoCpuWakeEvent
                (
                    wokenProcessName: wokenProcessName,
                    wokenPid: wokenPid,
                    wokenThreadName: wokenThreadName,
                    wokenTid: wokenTid,
                    wakerProcessName: wakerProcessName,
                    wakerPid: wakerPid,
                    wakerThreadName: wakerThreadName,
                    wakerTid: wakerTid,
                    timestamp: wake.StartTimestamp,
                    success: success == null ? -1 : (int)(long)success,
                    cpu: wake.Cpu,
                    targetCpu: target_cpu == null ? -1 : (int)(long)target_cpu,
                    priority: prio == null ? -1 : (int)(long)prio
                );

                this.CpuWakeEvents.AddEvent(ev);
            }

            this.CpuWakeEvents.FinalizeData();
        }

        /// <summary>
        /// Returns the wake event for the given just before the schedule timestamp.
        /// </summary>
        /// <param name="cpuWakeEvents">Timestamp sorted wake events for the woken thread.</param>
        /// <param name="time">Scheduling timestamp of the thread</param>
        /// <returns>CPU wake event if exists else null</returns>
        PerfettoCpuWakeEvent GetWakeEvent(IList<PerfettoCpuWakeEvent> cpuWakeEvents, Timestamp time)
        {
            int min = 0;
            int max = cpuWakeEvents.Count;

            while (min < max)
            {
                int mid = (min + max) / 2;

                if (cpuWakeEvents[mid].Timestamp <= time &&
                    (mid == 0 || // first
                    mid == max - 1 || // last
                    (mid + 1 < max && cpuWakeEvents[mid + 1].Timestamp > time))) // next one is greater
                {
                    return cpuWakeEvents[mid];
                }
                else if (cpuWakeEvents[mid].Timestamp > time)
                {
                    max = mid;
                }
                else
                {
                    min = mid + 1;
                }
            }

            return null;
        }
    }
}
