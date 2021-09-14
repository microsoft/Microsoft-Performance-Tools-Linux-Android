﻿// Copyright (c) Microsoft Corporation.
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
            PerfettoPluginConstants.ProcessCookerPath,
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
                            foreach(var provider in result.EventProviders)
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
            var processData = requiredData.QueryOutput<ProcessedEventData<PerfettoProcessEvent>>(new DataOutputPath(PerfettoPluginConstants.ProcessCookerPath, nameof(PerfettoProcessCooker.ProcessEvents)));
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
                         join threadTrack in threadTrackData on slice.TrackId equals threadTrack.Id into ttd from threadTrack in ttd.DefaultIfEmpty()
                         join thread in threadData on threadTrack?.Utid equals thread.Utid into td from thread in td.DefaultIfEmpty()
                         join threadProcess in processData on thread?.Upid equals threadProcess.Upid into pd from threadProcess in pd.DefaultIfEmpty()
                         join processTrack in processTrackData on slice.TrackId equals processTrack.Id into ptd from processTrack in ptd.DefaultIfEmpty()
                         join process in processData on processTrack?.Upid equals process.Upid into pd2 from process in pd2.DefaultIfEmpty()
                         select new { slice, args, threadTrack, thread, threadProcess, process };

            var longestRelTS = joined.Max(f => f.slice?.RelativeTimestamp);
            var longestEndTs = longestRelTS.HasValue ? new Timestamp(longestRelTS.Value) : Timestamp.MaxValue;

            // Create events out of the joined results
            foreach (var result in joined)
            {
                MaximumEventFieldCount = Math.Max(MaximumEventFieldCount, result.args.Count());

                string provider = string.Empty;
                List<string> argKeys = new List<string>();
                List<string> values = new List<string>();
                // Each event has multiple of these "debug annotations". They get stored in lists
                foreach (var arg in result.args)
                {
                    argKeys.Add(arg.ArgKey);
                    switch (arg.ValueType)
                    {
                        case "json":
                        case "string":
                            values.Add(arg.StringValue);

                            // Check if there are mappings present and if the arg key is the keyword we're looking for
                            if (ProviderGuidMapping.Count > 0 && arg.ArgKey.ToLower().Contains(ProviderDebugAnnotationKey))
                            {
                                // The value for this key was flagged as containing a provider GUID that needs to be mapped to its provider name
                                // Check if the mapping exists
                                Guid guid = new Guid(arg.StringValue);
                                if (ProviderGuidMapping.ContainsKey(guid))
                                {
                                    HasProviders = true;
                                    provider = ProviderGuidMapping[guid];
                                }
                            }
                            break;
                        case "bool":
                        case "int":
                            values.Add(arg.IntValue.ToString());
                            break;
                        case "uint":
                        case "pointer":
                            values.Add(((uint)arg.IntValue).ToString());
                            break;
                        case "real":
                            values.Add(arg.RealValue.ToString());
                            break;
                        default:
                            throw new Exception("Unexpected Perfetto value type");
                    }
                }

                string processName = string.Empty;
                string threadName = string.Empty;

                // An event can have a thread+process or just a process
                if (result.threadProcess != null)
                {
                    processName = $"{result.threadProcess.Name} {result.threadProcess.Pid}";
                    threadName = $"{result.thread.Name} {result.thread.Tid}";
                }
                if (result.process != null)
                {
                    processName = $"{result.process.Name} {result.process.Pid}";
                }

                int parentTreeDepthLevel = 0;
                long? currentParentId = result.slice.ParentId;
                List<string> tmpParentEventNameTreeBranch = new List<string>();
                tmpParentEventNameTreeBranch.Add(result.slice.Name);

                // Walk the parent tree
                while (currentParentId.HasValue)
                {
                    var parentPerfettoSliceEvent = sliceData[(int) currentParentId.Value];
                    // Debug.Assert(parentPerfettoSliceEvent == null || (parentPerfettoSliceEvent.Id == currentParentId.Value)); // Should be guaranteed by slice Id ordering. Since we are relying on index being the Id
                    
                    if (parentPerfettoSliceEvent != null)
                    {
                        currentParentId = parentPerfettoSliceEvent.ParentId;
                        tmpParentEventNameTreeBranch.Add(parentPerfettoSliceEvent.Name);
                    }
                    else
                    {
                        currentParentId =  null;
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
                   result.slice.Name,
                   result.slice.Type,
                   new TimestampDelta(result.slice.Duration),
                   new Timestamp(result.slice.RelativeTimestamp),
                   result.slice.Duration >= 0 ?             // Duration can be not complete / negative
                    new Timestamp(result.slice.RelativeTimestamp + result.slice.Duration) :
                    longestEndTs,
                   result.slice.Category,
                   result.slice.ArgSetId,
                   values,
                   argKeys,
                   processName,
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