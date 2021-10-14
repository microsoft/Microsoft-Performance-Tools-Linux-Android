using Microsoft.Performance.SDK;
using Microsoft.Performance.SDK.Extensibility;
using Microsoft.Performance.SDK.Extensibility.DataCooking.SourceDataCooking;
using PerfettoCds.Pipeline.Events;
using System;
using System.Collections.Generic;
using System.Threading;

namespace PerfettoCds.Pipeline.SourceDataCookers
{
    class PerfettoInstantCooker : BaseSourceDataCooker<PerfettoSqlEventKeyed, PerfettoSourceParser, string>
    {
        public PerfettoInstantCooker() : base(PerfettoPluginConstants.InstantCookerPath)
        {

        }

        public override string Description => "Processes events from the instant Perfetto SQL table";

        // add the output
        public IEnumerable<PerfettoInstantCooker> Instants;

        public override ReadOnlyHashSet<string> DataKeys => throw new NotImplementedException();

        public override DataProcessingResult CookDataElement(PerfettoSqlEventKeyed data, PerfettoSourceParser context, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
