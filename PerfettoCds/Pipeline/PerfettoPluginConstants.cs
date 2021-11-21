// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Microsoft.Performance.SDK.Extensibility;
using PerfettoCds.Pipeline.CompositeDataCookers;
using PerfettoCds.Pipeline.SourceDataCookers;
using PerfettoCds.Pipeline.DataOutput;
using PerfettoProcessor;

namespace PerfettoCds
{
    public static class PerfettoPluginConstants
    {
        public const string TraceProcessorShellFileName = @"trace_processor_shell.exe";

        // ID for source parser
        public const string ParserId = "PerfettoSourceParser";

        // ID for source data cookers
        public const string SliceCookerId = nameof(PerfettoSliceCooker);
        public const string ArgCookerId = nameof(PerfettoArgCooker);
        public const string ThreadCookerId = nameof(PerfettoThreadCooker);
        public const string ThreadTrackCookerId = nameof(PerfettoThreadTrackCooker);
        public const string ProcessCookerId = nameof(PerfettoProcessCooker);
        public const string SchedSliceCookerId = nameof(PerfettoSchedSliceCooker);
        public const string AndroidLogCookerId = nameof(PerfettoAndroidLogCooker);
        public const string RawCookerId = nameof(PerfettoRawCooker);
        public const string CounterCookerId = nameof(PerfettoCounterCooker);
        public const string CpuCounterTrackCookerId = nameof(PerfettoCpuCounterTrackCooker);
        public const string GpuCounterTrackCookerId = nameof(PerfettoGpuCounterTrackCooker);
        public const string ProcessCounterTrackCookerId = nameof(PerfettoProcessCounterTrackCooker);
        public const string CounterTrackCookerId = nameof(PerfettoCounterTrackCooker);
        public const string ProcessTrackCookerId = nameof(PerfettoProcessTrackCooker);
        public const string PerfSampleCookerId = nameof(PerfettoPerfSampleCooker);
        public const string StackProfileCallSiteCookerId = nameof(PerfettoStackProfileCallSiteCooker);
        public const string StackProfileFrameCookerId = nameof(PerfettoStackProfileFrameCooker);
        public const string StackProfileMappingCookerId = nameof(PerfettoStackProfileMappingCooker);
        public const string StackProfileSymbolCookerId = nameof(PerfettoStackProfileSymbolCooker);

        // ID for composite data cookers
        public const string GenericEventCookerId = nameof(PerfettoGenericEventCooker);
        public const string CpuSchedEventCookerId = nameof(PerfettoCpuSchedEventCooker);
        public const string LogcatEventCookerId = nameof(PerfettoLogcatEventCooker);
        public const string FtraceEventCookerId = nameof(PerfettoFtraceEventCooker);
        public const string CpuFrequencyEventCookerId = nameof(PerfettoCpuFrequencyEventCooker);
        public const string CpuCountersEventCookerId = nameof(PerfettoCpuCountersEventCooker);
        public const string GpuCountersEventCookerId = nameof(PerfettoGpuCountersEventCooker);
        public const string ProcessMemoryEventCookerId = nameof(PerfettoProcessMemoryEventCooker);
        public const string SystemMemoryEventCookerId = nameof(PerfettoSystemMemoryEventCooker);
        public const string CpuSamplingEventCookerId = nameof(PerfettoCpuSamplingEventCooker);

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
        public const string GpuCounterTrackEvent = PerfettoGpuCounterTrackEvent.Key;
        public const string ProcessCounterTrackEvent = PerfettoProcessCounterTrackEvent.Key;
        public const string CounterTrackEvent = PerfettoCounterTrackEvent.Key;
        public const string ProcessTrackEvent = PerfettoProcessTrackEvent.Key;
        public const string PerfSampleEvent = PerfettoPerfSampleEvent.Key;
        public const string StackProfileCallSiteEvent = PerfettoStackProfileCallSiteEvent.Key;
        public const string StackProfileFrameEvent = PerfettoStackProfileFrameEvent.Key;
        public const string StackProfileMappingEvent = PerfettoStackProfileMappingEvent.Key;
        public const string StackProfileSymbolEvent = PerfettoStackProfileSymbolEvent.Key;

        // Output events for composite cookers
        public const string GenericEvent = nameof(PerfettoGenericEvent);
        public const string CpuSchedEvent = nameof(PerfettoCpuSchedEvent);
        public const string LogcatEvent = nameof(PerfettoLogcatEvent);
        public const string FtraceEvent = nameof(PerfettoFtraceEvent);
        public const string CpuFrequencyEvent = nameof(PerfettoCpuFrequencyEvent);
        public const string CpuCountersEvent = nameof(PerfettoCpuCountersEvent);
        public const string GpuCountersEvent = nameof(PerfettoGpuCountersEvent);
        public const string ProcessMemoryEvent = nameof(PerfettoProcessMemoryEvent);
        public const string SystemMemoryEvent = nameof(PerfettoSystemMemoryEvent);
        public const string CpuSamplingEvent = nameof(PerfettoCpuSamplingEvent);

        // Paths for source cookers
        public static readonly DataCookerPath SliceCookerPath =
            DataCookerPath.ForSource(PerfettoPluginConstants.ParserId, PerfettoPluginConstants.SliceCookerId);
        public static readonly DataCookerPath ArgCookerPath =
            DataCookerPath.ForSource(PerfettoPluginConstants.ParserId, PerfettoPluginConstants.ArgCookerId);
        public static readonly DataCookerPath ThreadTrackCookerPath =
            DataCookerPath.ForSource(PerfettoPluginConstants.ParserId, PerfettoPluginConstants.ThreadTrackCookerId);
        public static readonly DataCookerPath ThreadCookerPath =
            DataCookerPath.ForSource(PerfettoPluginConstants.ParserId, PerfettoPluginConstants.ThreadCookerId);
        public static readonly DataCookerPath ProcessCookerPath =
            DataCookerPath.ForSource(PerfettoPluginConstants.ParserId, PerfettoPluginConstants.ProcessCookerId);
        public static readonly DataCookerPath SchedSliceCookerPath =
            DataCookerPath.ForSource(PerfettoPluginConstants.ParserId, PerfettoPluginConstants.SchedSliceCookerId);
        public static readonly DataCookerPath AndroidLogCookerPath =
            DataCookerPath.ForSource(PerfettoPluginConstants.ParserId, PerfettoPluginConstants.AndroidLogCookerId);
        public static readonly DataCookerPath RawCookerPath =
            DataCookerPath.ForSource(PerfettoPluginConstants.ParserId, PerfettoPluginConstants.RawCookerId);
        public static readonly DataCookerPath CounterCookerPath =
            DataCookerPath.ForSource(PerfettoPluginConstants.ParserId, PerfettoPluginConstants.CounterCookerId);
        public static readonly DataCookerPath CpuCounterTrackCookerPath =
            DataCookerPath.ForSource(PerfettoPluginConstants.ParserId, PerfettoPluginConstants.CpuCounterTrackCookerId);
        public static readonly DataCookerPath GpuCounterTrackCookerPath =
            DataCookerPath.ForSource(PerfettoPluginConstants.ParserId, PerfettoPluginConstants.GpuCounterTrackCookerId);
        public static readonly DataCookerPath ProcessCounterTrackCookerPath =
            DataCookerPath.ForSource(PerfettoPluginConstants.ParserId, PerfettoPluginConstants.ProcessCounterTrackCookerId);
        public static readonly DataCookerPath CounterTrackCookerPath =
            DataCookerPath.ForSource(PerfettoPluginConstants.ParserId, PerfettoPluginConstants.CounterTrackCookerId);
        public static readonly DataCookerPath ProcessTrackCookerPath =
            DataCookerPath.ForSource(PerfettoPluginConstants.ParserId, PerfettoPluginConstants.ProcessTrackCookerId);
        public static readonly DataCookerPath PerfSampleCookerPath =
            DataCookerPath.ForSource(PerfettoPluginConstants.ParserId, PerfettoPluginConstants.PerfSampleCookerId);
        public static readonly DataCookerPath StackProfileCallSiteCookerPath =
            DataCookerPath.ForSource(PerfettoPluginConstants.ParserId, PerfettoPluginConstants.StackProfileCallSiteCookerId);
        public static readonly DataCookerPath StackProfileFrameCookerPath =
            DataCookerPath.ForSource(PerfettoPluginConstants.ParserId, PerfettoPluginConstants.StackProfileFrameCookerId);
        public static readonly DataCookerPath StackProfileMappingCookerPath =
            DataCookerPath.ForSource(PerfettoPluginConstants.ParserId, PerfettoPluginConstants.StackProfileMappingCookerId);
        public static readonly DataCookerPath StackProfileSymbolCookerPath =
            DataCookerPath.ForSource(PerfettoPluginConstants.ParserId, PerfettoPluginConstants.StackProfileSymbolCookerId);

        // Paths for composite cookers
        public static readonly DataCookerPath GenericEventCookerPath =
            DataCookerPath.ForComposite(PerfettoPluginConstants.GenericEventCookerId);
        public static readonly DataCookerPath CpuSchedEventCookerPath =
            DataCookerPath.ForComposite(PerfettoPluginConstants.CpuSchedEventCookerId);
        public static readonly DataCookerPath LogcatEventCookerPath =
            DataCookerPath.ForComposite(PerfettoPluginConstants.LogcatEventCookerId);
        public static readonly DataCookerPath FtraceEventCookerPath =
            DataCookerPath.ForComposite(PerfettoPluginConstants.FtraceEventCookerId);
        public static readonly DataCookerPath CpuFrequencyEventCookerPath =
            DataCookerPath.ForComposite(PerfettoPluginConstants.CpuFrequencyEventCookerId);
        public static readonly DataCookerPath CpuCountersEventCookerPath =
            DataCookerPath.ForComposite(PerfettoPluginConstants.CpuCountersEventCookerId);
        public static readonly DataCookerPath GpuCountersEventCookerPath =
            DataCookerPath.ForComposite(PerfettoPluginConstants.GpuCountersEventCookerId);
        public static readonly DataCookerPath ProcessMemoryEventCookerPath =
            DataCookerPath.ForComposite(PerfettoPluginConstants.ProcessMemoryEventCookerId);
        public static readonly DataCookerPath SystemMemoryEventCookerPath =
            DataCookerPath.ForComposite(PerfettoPluginConstants.SystemMemoryEventCookerId);
        public static readonly DataCookerPath CpuSamplingEventCookerPath =
            DataCookerPath.ForComposite(PerfettoPluginConstants.CpuSamplingEventCookerId);
    }
}
