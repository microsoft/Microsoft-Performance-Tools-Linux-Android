// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Microsoft.Performance.SDK.Extensibility;
using PerfettoProcessor;

namespace PerfettoCds
{
    public static class PerfettoPluginConstants
    {
        public const string TraceProcessorShellFileName = @"trace_processor_shell.exe";

        // ID for source parser
        public const string ParserId = "PerfettoSourceParser";

        // ID for source data cookers
        public const string SliceCookerId = "PerfettoSliceCooker";
        public const string ArgCookerId = "PerfettoArgCooker";
        public const string ThreadCookerId = "PerfettoThreadCooker";
        public const string ThreadTrackCookerId = "PerfettoThreadCookerId";
        public const string ProcessCookerId = "PerfettoProcessCooker";
        public const string SchedSliceCookerId = "PerfettoSchedSliceCooker";
        public const string AndroidLogCookerId = "PerfettoAndroidLogCooker";
        public const string RawCookerId = "PerfettoRawCooker";
        public const string CounterCookerId = "PerfettoCounterCooker";
        public const string CpuCounterTrackCookerId = "PerfettoCpuCounterTrackCooker";

        // ID for composite data cookers
        public const string GenericEventCookerId = "PerfettoGenericEventCooker";
        public const string CpuSchedEventCookerId = "PerfettoCpuSchedEventCooker";
        public const string LogcatEventCookerId = "PerfettoLogcatEventCooker";
        public const string FtraceEventCookerId = "PerfettoFtraceEventCooker";
        public const string CpuFrequencyEventCookerId = "PerfettoCpuFrequencyEventCooker";

        // Events for source cookers
        public const string SliceEvent = PerfettoSliceEvent.Key;
        public const string ArgEvent = PerfettoArgEvent.Key;
        public const string ThreadTrackEvent = PerfettoThreadTrackEvent.Key;
        public const string ThreadEvent = PerfettoThreadEvent.Key;
        public const string ProcessEvent = PerfettoProcessEvent.Key;
        public const string SchedSliceEvent = PerfettoSchedSliceEvent.Key;
        public const string AndroidLogEvent = PerfettoAndroidLogEvent.Key;
        public const string RawEvent = PerfettoRawEvent.Key;
        public const string CounterEvent = PerfettoCounterEvent.Key;
        public const string CpuCounterTrackEvent = PerfettoCpuCounterTrackEvent.Key;

        // Output events for composite cookers
        public const string GenericEvent = "PerfettoGenericEvent";
        public const string CpuSchedEvent = "PerfettoCpuSchedEvent";
        public const string LogcatEvent = "PerfettoLogcatEvent";
        public const string FtraceEvent = "PerfettoFtraceEvent";
        public const string CpuFrequencyEvent = "PerfettoCpuFrequencyEvent";

        // Paths for source cookers
        public static readonly DataCookerPath SliceCookerPath =
            new DataCookerPath(PerfettoPluginConstants.ParserId, PerfettoPluginConstants.SliceCookerId);
        public static readonly DataCookerPath ArgCookerPath =
            new DataCookerPath(PerfettoPluginConstants.ParserId, PerfettoPluginConstants.ArgCookerId);
        public static readonly DataCookerPath ThreadTrackCookerPath =
            new DataCookerPath(PerfettoPluginConstants.ParserId, PerfettoPluginConstants.ThreadTrackCookerId);
        public static readonly DataCookerPath ThreadCookerPath =
            new DataCookerPath(PerfettoPluginConstants.ParserId, PerfettoPluginConstants.ThreadCookerId);
        public static readonly DataCookerPath ProcessCookerPath =
            new DataCookerPath(PerfettoPluginConstants.ParserId, PerfettoPluginConstants.ProcessCookerId);
        public static readonly DataCookerPath SchedSliceCookerPath =
            new DataCookerPath(PerfettoPluginConstants.ParserId, PerfettoPluginConstants.SchedSliceCookerId);
        public static readonly DataCookerPath AndroidLogCookerPath =
            new DataCookerPath(PerfettoPluginConstants.ParserId, PerfettoPluginConstants.AndroidLogCookerId);
        public static readonly DataCookerPath RawCookerPath =
            new DataCookerPath(PerfettoPluginConstants.ParserId, PerfettoPluginConstants.RawCookerId);
        public static readonly DataCookerPath CounterCookerPath =
            new DataCookerPath(PerfettoPluginConstants.ParserId, PerfettoPluginConstants.CounterCookerId);
        public static readonly DataCookerPath CpuCounterTrackCookerPath =
            new DataCookerPath(PerfettoPluginConstants.ParserId, PerfettoPluginConstants.CpuCounterTrackCookerId);

        // Paths for composite cookers
        public static readonly DataCookerPath GenericEventCookerPath =
            new DataCookerPath(PerfettoPluginConstants.GenericEventCookerId);
        public static readonly DataCookerPath CpuSchedEventCookerPath =
            new DataCookerPath(PerfettoPluginConstants.CpuSchedEventCookerId);
        public static readonly DataCookerPath LogcatEventCookerPath =
            new DataCookerPath(PerfettoPluginConstants.LogcatEventCookerId);
        public static readonly DataCookerPath FtraceEventCookerPath =
            new DataCookerPath(PerfettoPluginConstants.FtraceEventCookerId);
        public static readonly DataCookerPath CpuFrequencyEventCookerPath =
            new DataCookerPath(PerfettoPluginConstants.CpuFrequencyEventCookerId);

    }
}
