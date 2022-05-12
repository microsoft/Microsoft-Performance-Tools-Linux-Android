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
        public static Dictionary<string, object> ParseArgs(IEnumerable<PerfettoArgEvent> perfettoArgEvents)
        {
            var args = new Dictionary<string, object>();

            // Each event has multiple of these "debug annotations". They get stored in lists
            foreach (var arg in perfettoArgEvents)
            {
                switch (arg.ValueType)
                {
                    case "json":
                    case "string":
                        args.Add(arg.ArgKey, Common.StringIntern(arg.StringValue));
                        break;
                    case "bool":
                    case "int":
                        args.Add(arg.ArgKey, arg.IntValue);
                        break;
                    case "uint":
                    case "pointer":
                        args.Add(arg.ArgKey, (uint)arg.IntValue);
                        break;
                    case "real":
                        args.Add(arg.ArgKey, arg.RealValue);
                        break;
                    case "null":
                        args.Add(arg.ArgKey, null);
                        break;
                    default:
                        throw new Exception("Unexpected Perfetto value type");
                }
            }

            return args;
        }
    }
}
