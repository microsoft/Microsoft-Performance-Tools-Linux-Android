// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Serialization;
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
    /// XML deserialized EventProvider
    /// </summary>
    public class EventProvider
    {
        [XmlAttribute("Id")]
        public string Provider { get; set; }
        [XmlAttribute("Name")]
        public string Guid { get; set; }
    }

    /// <summary>
    /// Deserialized root of the ProviderGUID XML file
    /// </summary>
    [XmlRoot("EventProviders")]
    public class EventProvidersRoot
    {
        [XmlElement("EventProvider")]
        public EventProvider[] EventProviders { get; set; }

        [XmlAttribute("DebugAnnotationKey")]
        public string DebugAnnotationKey { get; set; }
    }

    /// <summary>
    /// Pulls data from multiple individual SQL tables and joins them to create a Generic Peretto event
    /// </summary>
    public sealed class PerfettoGenericEventCooker : CookedDataReflector, ICompositeDataCookerDescriptor
    {
        public static readonly DataCookerPath DataCookerPath = PerfettoPluginConstants.GenericEventCookerPath;

        public string Description => "Generic Event composite cooker";
        const string Root = "[Root]";

        public DataCookerPath Path => DataCookerPath;

        // Declare all of the cookers that are used by this CompositeCooker.
        public IReadOnlyCollection<DataCookerPath> RequiredDataCookers => new[]
        {
            PerfettoPluginConstants.SliceCookerPath,
            PerfettoPluginConstants.ArgCookerPath,
            PerfettoPluginConstants.ThreadTrackCookerPath,
            PerfettoPluginConstants.ThreadCookerPath,
            PerfettoPluginConstants.ProcessEventCookerPath,
            PerfettoPluginConstants.ProcessTrackCookerPath
        };

        [DataOutput]
        public ProcessedEventData<PerfettoGenericEvent> GenericEvents { get; }

        /// <summary>
        /// The highest number of fields found in any single event.
        /// </summary>
        [DataOutput]
        public int MaximumEventFieldCount { get; private set; }

        /// <summary>
        /// Whether or not there are any provider fields set
        /// </summary>
        [DataOutput]
        public bool HasProviders { get; private set; }

        /// <summary>
        /// A mapping between a provider GUID and the provider string name
        /// </summary>
        private Dictionary<Guid, string> ProviderGuidMapping;

        // Look for this debug annotation key when looking for provider GUIDs
        private string ProviderDebugAnnotationKey = "providerguid";

        // The name of the optional ProviderGuid mapping file
        private const string ProviderMappingXmlFilename = "ProviderMapping.xml";

        // In order to consolidate like paths for common events and thus reduce memory / processing
        private Dictionary<int, string[]> ParentEventNameTreeBranchDictKeyNotReversed = new Dictionary<int, string[]>();

        public PerfettoGenericEventCooker() : base(PerfettoPluginConstants.GenericEventCookerPath)
        {
            this.GenericEvents =
                new ProcessedEventData<PerfettoGenericEvent>();
            this.ProviderGuidMapping = new Dictionary<Guid, string>();

            TryLoadProviderGuidXml();
        }

        /// <summary>
        /// The user can specify a ProviderMapping.xml file that contains a mapping between a Provider name and the Provider GUID
        /// Check for the file in the assembly directory and load the mappings
        /// </summary>
        private void TryLoadProviderGuidXml()
        {
            var pluginDir = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var providerMappingXmlFile = System.IO.Path.Combine(pluginDir, ProviderMappingXmlFilename);

            if (File.Exists(providerMappingXmlFile))
            {
                try
                {
                    using (StreamReader reader = new StreamReader(providerMappingXmlFile))
                    {
                        XmlSerializer serializer = new XmlSerializer(typeof(EventProvidersRoot));
                        var result = (EventProvidersRoot)serializer.Deserialize(reader);

                        // The user can set an optional custom debug annotation key
                        if (result.DebugAnnotationKey != null)
                        {
                            this.ProviderDebugAnnotationKey = result.DebugAnnotationKey.ToLower();
                        }

                        if (result.EventProviders.Length == 0)
                        {
                            Console.Error.WriteLine($"Error: No Provider GUID entries found. Please check your {ProviderMappingXmlFilename}");
                        }
                        else
                        {
                            foreach (var provider in result.EventProviders)
                            {
                                this.ProviderGuidMapping.Add(new Guid(provider.Guid), provider.Provider);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine($"Error processing Provider GUID XML file: {e.Message}");
                }
            }
        }

        public void OnDataAvailable(IDataExtensionRetrieval requiredData)
        {
            // Gather the data from all the SQL tables
            var sliceData = requiredData.QueryOutput<ProcessedEventData<PerfettoSliceEvent>>(new DataOutputPath(PerfettoPluginConstants.SliceCookerPath, nameof(PerfettoSliceCooker.SliceEvents)));
            var argData = requiredData.QueryOutput<ProcessedEventData<PerfettoArgEvent>>(new DataOutputPath(PerfettoPluginConstants.ArgCookerPath, nameof(PerfettoArgCooker.ArgEvents)));
            var threadTrackData = requiredData.QueryOutput<ProcessedEventData<PerfettoThreadTrackEvent>>(new DataOutputPath(PerfettoPluginConstants.ThreadTrackCookerPath, nameof(PerfettoThreadTrackCooker.ThreadTrackEvents)));
            var threadData = requiredData.QueryOutput<ProcessedEventData<PerfettoThreadEvent>>(new DataOutputPath(PerfettoPluginConstants.ThreadCookerPath, nameof(PerfettoThreadCooker.ThreadEvents)));
            var processData = requiredData.QueryOutput<ProcessedEventData<PerfettoProcessEvent>>(new DataOutputPath(PerfettoPluginConstants.ProcessEventCookerPath, nameof(PerfettoProcessEventCooker.ProcessEvents)));
            var processTrackData = requiredData.QueryOutput<ProcessedEventData<PerfettoProcessTrackEvent>>(new DataOutputPath(PerfettoPluginConstants.ProcessTrackCookerPath, nameof(PerfettoProcessTrackCooker.ProcessTrackEvents)));

            // Join them all together

            // Slice data contains event name and a few more fields
            // Arg data contains the debug annotations
            // ThreadTrack data allows us to get to the thread
            // Thread data gives us the thread name+ID and gets us the process
            // Process data gives us the process name+ID
            // ProcessTrack gives us events that only have a process and not a thread
            var joined = from slice in sliceData
                         join arg in argData on slice.ArgSetId equals arg.ArgSetId into args
                         join threadTrack in threadTrackData on slice.TrackId equals threadTrack.Id into ttd
                         from threadTrack in ttd.DefaultIfEmpty()
                         join thread in threadData on threadTrack?.Utid equals thread.Utid into td
                         from thread in td.DefaultIfEmpty()
                         join threadProcess in processData on thread?.Upid equals threadProcess.Upid into pd
                         from threadProcess in pd.DefaultIfEmpty()
                         join threadProcessProcess in processData on threadProcess?.Upid equals threadProcessProcess.Upid into pd1
                         from threadProcessProcess in pd1.DefaultIfEmpty()
                         join processTrack in processTrackData on slice.TrackId equals processTrack.Id into ptd
                         from processTrack in ptd.DefaultIfEmpty()
                         join processTrackProcess in processData on processTrack?.Upid equals processTrackProcess.Upid into pd2
                         from processTrackProcess in pd2.DefaultIfEmpty()
                         select new { slice, args, threadTrack, thread, threadProcess, threadProcessProcess, processTrackProcess };

            var longestRelTS = joined.Max(f => f.slice?.RelativeTimestamp);
            var longestEndTs = longestRelTS.HasValue ? new Timestamp(longestRelTS.Value) : Timestamp.MaxValue;

            Dictionary<int, long> SliceId_DurationExclusive = new Dictionary<int, long>();
            // Duration Exclusion calculation (Duration minus child durations)
            // First we need to walk all the events & their direct parent
            // Slices seem to be per-thread in Perfetto and thus are non-overlapping (time-wise work)
            // Thus we can just subtract children time
            foreach (var result in joined)
            {
                int? parentId = result.slice.ParentId;

                if (!SliceId_DurationExclusive.ContainsKey(result.slice.Id))
                {
                    SliceId_DurationExclusive[result.slice.Id] = result.slice.Duration;
                }

                if (parentId.HasValue)
                {
                    var parentPerfettoSliceEvent = sliceData[parentId.Value];
                    if (parentPerfettoSliceEvent != null)
                    {
                        if (SliceId_DurationExclusive.TryGetValue(parentId.Value, out long currentParentExDuration))
                        {
                            // Some slices have negative duration and we don't want to increase exclusive duration of parent for this
                            if (result.slice.Duration > 0 && currentParentExDuration > 0)
                            {
                                SliceId_DurationExclusive[parentId.Value] = currentParentExDuration - result.slice.Duration;
                            }
                            Debug.Assert(SliceId_DurationExclusive[parentId.Value] >= -1);  // Verify non-overlapping otherwise duration will go negative excluding bad durations
                        }
                        else
                        {
                            SliceId_DurationExclusive.Add(parentId.Value, parentPerfettoSliceEvent.Duration);
                        }
                    }
                }
            }

            // Create events out of the joined results
            foreach (var result in joined)
            {
                MaximumEventFieldCount = Math.Max(MaximumEventFieldCount, result.args.Count());
                var args = Args.ParseArgs(result.args);
                string provider = string.Empty;

                // Each event has multiple of these "debug annotations". They get stored in lists
                foreach (var arg in args)
                {
                    // Check if there are mappings present and if the arg key is the keyword we're looking for
                    if (ProviderGuidMapping.Count > 0 && arg.Key.ToLower().Contains(ProviderDebugAnnotationKey))
                    {
                        // The value for this key was flagged as containing a provider GUID that needs to be mapped to its provider name
                        // Check if the mapping exists
                        if (Guid.TryParse(arg.Value.ToString(), out Guid guid))
                        {
                            if (ProviderGuidMapping.ContainsKey(guid))
                            {
                                HasProviders = true;
                                provider = ProviderGuidMapping[guid];
                            }
                        }
                    }
                }

                string processName = string.Empty;
                string processLabel = string.Empty;
                string threadName = string.Empty;
                if (result.thread != null)
                {
                    threadName = $"{result.thread.Name} ({result.thread.Tid})";
                }

                // An event can have a thread+process or just a process
                if (result.threadProcess != null)
                {
                    processName = $"{result.threadProcess.Name} ({result.threadProcess.Pid})";
                }
                else if (result.processTrackProcess != null)
                {
                    processName = $"{result.processTrackProcess.Name} ({result.processTrackProcess.Pid})";
                }

                if (result.threadProcessProcess != null)
                {
                    processLabel = result.threadProcessProcess.Label;
                }
                else if (result.processTrackProcess != null)
                {
                    processLabel = result.processTrackProcess.Label;
                }

                int parentTreeDepthLevel = 0;
                int? currentParentId = result.slice.ParentId;
                List<string> tmpParentEventNameTreeBranch = new List<string>();
                tmpParentEventNameTreeBranch.Add(result.slice.Name);

                // Walk the parent tree
                while (currentParentId.HasValue)
                {
                    var parentPerfettoSliceEvent = sliceData[currentParentId.Value];
                    // Debug.Assert(parentPerfettoSliceEvent == null || (parentPerfettoSliceEvent.Id == currentParentId.Value)); // Should be guaranteed by slice Id ordering. Since we are relying on index being the Id

                    if (parentPerfettoSliceEvent != null)
                    {
                        currentParentId = parentPerfettoSliceEvent.ParentId;
                        tmpParentEventNameTreeBranch.Add(parentPerfettoSliceEvent.Name);
                    }
                    else
                    {
                        currentParentId = null;
                    }

                    parentTreeDepthLevel++;
                }
                tmpParentEventNameTreeBranch.Add(Root);

                string[] finalParentEventNameTreeBranch;
                var tmpParentEventNameTreeBranchHashCodeNotReversed = tmpParentEventNameTreeBranch.GetHashCode();
                if (!ParentEventNameTreeBranchDictKeyNotReversed.TryGetValue(tmpParentEventNameTreeBranchHashCodeNotReversed, out finalParentEventNameTreeBranch))
                {
                    tmpParentEventNameTreeBranch.Reverse();
                    finalParentEventNameTreeBranch = tmpParentEventNameTreeBranch.ToArray();
                    ParentEventNameTreeBranchDictKeyNotReversed.Add(tmpParentEventNameTreeBranchHashCodeNotReversed, finalParentEventNameTreeBranch);
                }

                PerfettoGenericEvent ev = new PerfettoGenericEvent
                (
                   result.slice.Id,
                   result.slice.Name,
                   result.slice.Type,
                   new TimestampDelta(result.slice.Duration),
                   new TimestampDelta(SliceId_DurationExclusive[result.slice.Id]),
                   new Timestamp(result.slice.RelativeTimestamp),
                   result.slice.Duration >= 0 ?             // Duration can be not complete / negative
                    new Timestamp(result.slice.RelativeTimestamp + result.slice.Duration) :
                    longestEndTs,
                   result.slice.Category,
                   args,
                   processName,
                   processLabel,
                   threadName,
                   provider,
                   result.threadTrack,
                   result.slice.ParentId,
                   parentTreeDepthLevel,
                   finalParentEventNameTreeBranch
                );
                this.GenericEvents.AddEvent(ev);
            }
            this.GenericEvents.FinalizeData();
        }
    }
}