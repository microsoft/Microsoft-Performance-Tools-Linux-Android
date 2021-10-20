// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;
using PerfCds.CookerData;
using Microsoft.Performance.SDK;
using Microsoft.Performance.SDK.Extensibility;
using Microsoft.Performance.SDK.Extensibility.DataCooking;
using Microsoft.Performance.SDK.Extensibility.DataCooking.SourceDataCooking;
using System.Threading;
using CtfPlayback;
using PerfDataExtensions.SourceDataCookers.Symbols;
using System.IO;

namespace PerfDataExtensions.SourceDataCookers.Cpu
{
    public class PerfCpuClockDataCooker
            : PerfBaseSourceCooker
    {
        public const string Identifier = "CpuClockDataCooker";

        public PerfCpuClockDataCooker()
            : base(Identifier)
        {
        }

        public override string Description => "Processes Perf CTF events related to CPU Sampling (cpu-clock)";

        public override ReadOnlyHashSet<string> DataKeys => EmptyDataKeys;

        [DataOutput]
        public List<CpuClockEvent> CpuClockEvents { get; } = new List<CpuClockEvent>();

        private bool attemptedProcessSymbols = false;
        public SortedList<ulong, KernelSymbol> KernelSymbols { get; } = new SortedList<ulong, KernelSymbol>();

        private object symProcessLock = new object();

        /// <summary>
        /// This data cooker receives all data elements.
        /// </summary>
        public override SourceDataCookerOptions Options => SourceDataCookerOptions.ReceiveAllDataElements;

        public override DataProcessingResult CookDataElement(PerfEvent data, PerfContext context, CancellationToken cancellationToken)
        {
            lock (symProcessLock)
            {
                if (!attemptedProcessSymbols)
                {
                    try
                    {
                        // TODO: This is hard-coded for now. We don't seem to have a good way to know/infer the trace path or choose this in the UI. CTF SDK change needed?

                        var kallsymsFile = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "kallsyms");
                        if (File.Exists(kallsymsFile))
                        {
                            using (StreamReader sr = new StreamReader(kallsymsFile))
                            {
                                string line;
                                while ((line = sr.ReadLine()) != null)
                                {
                                    var ks = new KernelSymbol(line);

                                    if (KernelSymbols.ContainsKey(ks.Address))
                                    {
                                        Console.Out.WriteLine($"Unable to add {line}. There is already an entry this address");
                                    }
                                    else
                                    {
                                        KernelSymbols.Add(ks.Address, ks);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Exception processing symbols: {e.Message}");
                    }
                    finally
                    {
                        attemptedProcessSymbols = true;
                    }
                }
            }

            try
            {
                if (data.Name == "cpu-clock")
                {
                    CpuClockEvents.Add(new CpuClockEvent(data, context, KernelSymbols));
                    return DataProcessingResult.Processed;
                }
                else
                {
                    return DataProcessingResult.Ignored;
                }
            }
            catch (CtfPlaybackException e)
            {
                Console.Error.WriteLine(e);
                return DataProcessingResult.CorruptData;
            }
        }
    }
}
