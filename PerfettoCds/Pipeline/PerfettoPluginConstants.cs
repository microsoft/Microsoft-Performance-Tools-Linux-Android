// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Microsoft.Performance.SDK.Extensibility;

namespace PerfettoCds
{
    public static class PerfettoPluginConstants
    {
        public const string TraceProcessorShellPath = @"trace_processor_shell.exe";

        // ID for source parser
        public const string ParserId = "PerfettoSourceParser";

        // ID for source data cookers
        public const string SliceCookerId = "PerfettoSliceCooker";
        public const string ArgCookerId = "PerfettoArgCooker";
        public const string ThreadCookerId = "PerfettoThreadCooker";
        public const string ThreadTrackCookerId = "PerfettoThreadCookerId";
        public const string ProcessCookerId = "PerfettoProcessCooker";

        // ID for composite data cookers
        public const string GenericEventCookerId = "PerfettoGenericEventCooker";

        // Events for source cookers
        public const string SliceEvent = "PerfettoSliceEvent";
        public const string ArgEvent = "PerfettoArgEvent";
        public const string ThreadTrackEvent = "PerfettoThreadTrackEvent";
        public const string ThreadEvent = "PerfettoThreadEvent";
        public const string ProcessEvent = "PerfettoProcessEvent";

        // Output events for composite cookers
        public const string GenericEvent = "PerfettoGenericEvent";

        // Path from source parser to example data cooker. This is the path
        // that is used to programatically access the data cooker's data outputs,
        // and can be created by external binaries by just knowing the
        // parser and cooker IDs defined above
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

        public static readonly DataCookerPath GenericEventCookerPath =
            new DataCookerPath(PerfettoPluginConstants.GenericEventCookerId);
    }
}
