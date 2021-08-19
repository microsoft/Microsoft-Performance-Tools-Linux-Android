// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Performance.SDK;
using Microsoft.Performance.SDK.Extensibility;
using Microsoft.Performance.SDK.Extensibility.DataCooking;
using Microsoft.Performance.SDK.Processing;
using PerfettoCds.Pipeline.DataOutput;
using PerfettoProcessor;
using System.Xml;
using System.Xml.Serialization;
using System.IO;

namespace PerfettoCds.Pipeline.DataCookers
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

        public DataCookerPath Path => DataCookerPath;

        // Declare all of the cookers that are used by this CompositeCooker.
        public IReadOnlyCollection<DataCookerPath> RequiredDataCookers => new[]
        {
            PerfettoPluginConstants.SliceCookerPath,
            PerfettoPluginConstants.ArgCookerPath,
            PerfettoPluginConstants.ThreadTrackCookerPath,
            PerfettoPluginConstants.ThreadCookerPath,
            PerfettoPluginConstants.ProcessCookerPath
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

            // Join them all together

            // Slice data contains event name and a few more fields
            // Arg data contains the debug annotations
            // ThreadTrack data allows us to get to the thread
            // Thread data gives us the thread name+ID and gets us the process
            // Process data gives us the process name+ID
            var joined = from slice in sliceData
                         join arg in argData on slice.ArgSetId equals arg.ArgSetId into args
                         join threadTrack in threadTrackData on slice.TrackId equals threadTrack.Id
                         join thread in threadData on threadTrack.Utid equals thread.Utid
                         join process in processData on thread.Upid equals process.Upid
                         select new { slice, args, threadTrack, thread, process };

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
                PerfettoGenericEvent ev = new PerfettoGenericEvent
                (
                   result.slice.Name,
                   result.slice.Type,
                   new TimestampDelta(result.slice.Duration),
                   new Timestamp(result.slice.RelativeTimestamp),
                   new Timestamp(result.slice.RelativeTimestamp + result.slice.Duration),
                   result.slice.Category,
                   result.slice.ArgSetId,
                   values,
                   argKeys,
                   string.Format($"{result.process.Name} {result.process.Pid}"),
                   string.Format($"{result.thread.Name} {result.thread.Tid}"),
                   provider
                );
                this.GenericEvents.AddEvent(ev);
            }
            this.GenericEvents.FinalizeData();
        }
    }
}