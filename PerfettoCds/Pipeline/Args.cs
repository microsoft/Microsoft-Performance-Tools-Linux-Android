// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Performance.SDK.Processing;
using PerfettoProcessor;
using Utilities;

namespace PerfettoCds
{
    public class Args
    {
        public List<string> ArgKeys { get; private set; }
        public List<object> Values { get; private set; }

        public static Args ParseArgs(IEnumerable<PerfettoArgEvent> perfettoArgEvents)
        {
            var args = new Args();
            args.ArgKeys = new List<string>();
            args.Values = new List<object>();

            // Each event has multiple of these "debug annotations". They get stored in lists
            foreach (var arg in perfettoArgEvents)
            {
                args.ArgKeys.Add(Common.StringIntern(arg.ArgKey));
                switch (arg.ValueType)
                {
                    case "json":
                    case "string":
                        args.Values.Add(Common.StringIntern(arg.StringValue));
                        break;
                    case "bool":
                    case "int":
                        args.Values.Add(arg.IntValue);
                        break;
                    case "uint":
                    case "pointer":
                        args.Values.Add((uint)arg.IntValue);
                        break;
                    case "real":
                        args.Values.Add(arg.RealValue);
                        break;
                    default:
                        throw new Exception("Unexpected Perfetto value type");
                }
            }

            return args;
        }
    }
}
