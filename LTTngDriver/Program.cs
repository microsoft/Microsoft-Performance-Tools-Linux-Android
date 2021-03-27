// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using LTTngDataExtensions.DataOutputTypes;
using LTTngDataExtensions.SourceDataCookers;
using Microsoft.Performance.SDK.Extensibility;
using Microsoft.Performance.SDK.Processing;
using Microsoft.Performance.Toolkit.Engine;

namespace LTTngDriver
{
    public sealed partial class Program
    {
        private readonly string[] args;

        public Program(string[] args)
        {
            this.args = args ?? Array.Empty<string>();
        }

        public bool Run()
        {
            var parsed = Arguments.Parse(this.args);
            if (parsed is null ||
                parsed.ShowHelp)
            {
                Arguments.PrintUsage();
                return false;
            }

            //
            // Create our runtime environment, enabling cookers and
            // adding inputs.
            //

            Console.WriteLine($"ExtensionDirectory:{parsed.ExtensionDirectory}");

            var runtime = Engine.Create(
                new EngineCreateInfo
                {
                    ExtensionDirectory = parsed.ExtensionDirectory,
                });

            Debug.Assert(parsed.CtfInput != null);
            Debug.Assert(parsed.CtfInput.Count > 0);
            foreach (var ctf in parsed.CtfInput.Distinct())
            {
                Console.WriteLine($"CTF Path:{ctf}");
                runtime.AddFile(ctf);
            }

            var lttngGenericEventDataCooker = new LTTngGenericEventDataCooker();
            var cookerName = lttngGenericEventDataCooker.Path;
            runtime.EnableCooker(cookerName);

            //
            // Process our data.
            //

            var results = runtime.Process();

            //
            // Access our cooked data.
            //

            var eventData = results.QueryOutput<ProcessedEventData<LTTngGenericEvent>>(
                new DataOutputPath(
                    cookerName,
                    nameof(LTTngGenericEventDataCooker.Events)));

            TextWriter output = null;
            FileStream file = null;
            try
            {
                if (!string.IsNullOrWhiteSpace(parsed.Outfile))
                {
                    if (parsed.Force)
                    {
                        file = File.Create(parsed.Outfile);
                    }
                    else
                    {
                        file = File.OpenWrite(parsed.Outfile);
                    }

                    Debug.Assert(file != null);
                    output = new StreamWriter(file);
                }
                else
                {
                    output = Console.Out;
                }

                Debug.Assert(output != null);
                EmitEvents(eventData, output);
            }
            finally
            {
                if (output != null)
                {
                    if (output != Console.Out)
                    {
                        //
                        // This will also dispose out file stream
                        // as the writer takes ownership.
                        //

                        output.Flush();
                        output.Dispose();
                    }
                }
                else if (file != null)
                {
                    //
                    // We opened for write, but never created our output
                    // writer, so just close and delete.
                    //

                    file.Flush();
                    file.Dispose();

                    File.Delete(parsed.Outfile);
                }
                else
                {
                    //
                    // both are null, so there is nothing to clean up
                    //
                }
            }

            return true;
        }

        private static void EmitEvents(
            ProcessedEventData<LTTngGenericEvent> events,
            TextWriter output)
        {
            Debug.Assert(events != null);
            Debug.Assert(output != null);

            var genericEventProperties = typeof(LTTngGenericEvent).GetProperties();
            Debug.Assert(genericEventProperties != null);

            var maxNumFields = events.Select(x => x.FieldNames.Count).Max();

            var first = true;
            var indexProperties = new HashSet<PropertyInfo>();
            foreach (var p in genericEventProperties)
            {
                Debug.Assert(p != null);
                if (IsIndexProperty(p))
                {
                    //
                    // we have hit the indexer on LTTngGeneric Event. We will enumerate
                    // that separately after this property loop (they are the field N
                    // values, so it makes sense to do them separatly.)
                    //

                    indexProperties.Add(p);
                    continue;
                }

                if (!first)
                {
                    output.Write(',');
                }
                else
                {
                    first = false;
                }

                output.Write(p.Name);
            }

            for (var i = 0; i < maxNumFields; ++i)
            {
                output.Write(",Field {0}", i + 1);
            }

            output.WriteLine();

            foreach (var e in events)
            {
                first = true;
                foreach (var p in genericEventProperties.Where(x => !indexProperties.Contains(x)))
                {
                    if (!first)
                    {
                        output.Write(',');
                    }
                    else
                    {
                        first = false;
                    }

                    var v = p.GetValue(e);

                    if (IsList(v))
                    {
                        var list = v as IList;
                        var firstli = true;
                        foreach (var li in list)
                        {
                            if (!firstli)
                            {
                                output.Write(';');
                            }
                            else
                            {
                                firstli = false;
                            }
                            output.Write(li);
                        }
                    }
                    else
                    {
                        output.Write(v);
                    }
                }

                Debug.Assert(e.FieldNames.Count <= maxNumFields);
                for (var i = 0; i < e.FieldNames.Count; ++i)
                {
                    output.Write(",{0}", e.FieldValues[i]);
                }

                for (var i = e.FieldNames.Count; i < maxNumFields; ++i)
                {
                    output.Write(',');
                }

                output.WriteLine();
            }
        }

        public static bool IsList(object o)
        {
            if (o == null) return false;
            return o is IList &&
                   o.GetType().IsGenericType &&
                   o.GetType().GetGenericTypeDefinition().IsAssignableFrom(typeof(List<>));
        }

        private static bool IsIndexProperty(PropertyInfo p)
        {
            return (p?.GetIndexParameters()?.Length ?? 0) > 0;
        }

        private sealed class Arguments
        {
            private static readonly string ProgramName = Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName);

            public Arguments()
            {
                this.CtfInput = new List<string>();
            }

            public List<string> CtfInput { get; }

            public string ExtensionDirectory { get; set; }

            public bool Force { get; set; }

            public string Outfile { get; set; }

            public bool ShowHelp { get; set; }

            public static Arguments Parse(string[] args)
            {
                if (args is null)
                {
                    return null;
                }

                var parsed = new Arguments();

                var index = 0;
                var inFileMode = false;
                var bad = false;
                while (index < args.Length)
                {
                    var curr = args[index];
                    if (inFileMode)
                    {
                        if (curr != null)
                        {
                            parsed.CtfInput.Add(curr);
                        }
                    }
                    else
                    {
                        if (curr is null)
                        {
                            //
                            // just go around and ignore it.
                            // do nothing in this block.
                            //
                        }
                        else if (curr == "-?" ||
                            curr == "/?" ||
                            curr == "-h" ||
                            curr == "-help")
                        {
                            parsed.ShowHelp = true;

                            //
                            // The user asked for help, so immediately
                            // stop parsing.
                            //

                            break;
                        }
                        else if (curr == "-e")
                        {
                            if (!string.IsNullOrWhiteSpace(parsed.ExtensionDirectory))
                            {
                                EmitError("'-e' has been set multiple times.");
                                bad = true;
                            }
                            else if (index + 1 >= args.Length)
                            {
                                EmitError("'-e' is missing an argument");
                                bad = true;
                            }
                            else
                            {
                                Debug.Assert(index + 1 < args.Length);
                                var e = args[index + 1];
                                if (e == "--")
                                {
                                    EmitError("'-o' is missing an argument");
                                    bad = true;
                                }
                                else
                                {
                                    parsed.ExtensionDirectory = e;
                                }

                                ++index;
                            }
                        }
                        else if (curr == "-f")
                        {
                            parsed.Force = true;
                        }
                        else if (curr == "-o")
                        {
                            if (!string.IsNullOrWhiteSpace(parsed.Outfile))
                            {
                                EmitError("'-o' has been set multiple times.");
                                bad = true;
                            }
                            else if (index + 1 >= args.Length)
                            {
                                EmitError("'-o' is missing an argument");
                                bad = true;
                            }
                            else
                            {
                                Debug.Assert(index + 1 < args.Length);
                                var f = args[index + 1];
                                if (f != "-")
                                {
                                    //
                                    // '-' is often used to denote stdout, so
                                    // just ignore it if it is specified.
                                    //

                                    if (f == "--")
                                    {
                                        EmitError("'-o' is missing an argument");
                                        bad = true;
                                    }
                                    else
                                    {
                                        parsed.Outfile = f;
                                    }
                                }

                                ++index;
                            }
                        }
                        else if (curr == "--")
                        {
                            inFileMode = true;
                        }
                        else if (curr.StartsWith("-"))
                        {
                            Debug.Assert(!inFileMode);
                            EmitError("Invalid option '{0}'", curr);
                            bad = true;
                        }
                        else
                        {
                            Debug.Assert(curr != null);
                            parsed.CtfInput.Add(curr);
                            inFileMode = true;
                        }
                    }

                    ++index;
                }

                if (parsed.ShowHelp)
                {
                    return parsed;
                }

                if (parsed.CtfInput.Count == 0)
                {
                    EmitError("At least one CTF input must be specified.");
                    bad = true;
                }

                return !bad
                    ? parsed
                    : null;
            }

            public static void PrintUsage()
            {
                Console.Out.WriteLine("CTF Generic Dumper");
                Console.Out.WriteLine();
                Console.Out.WriteLine("usage: [{0}] [-e extension_dir] [-o out_file] [-f] [--] <CTF PATH>...", ProgramName);
                Console.Out.WriteLine("       [{0}] -h|-help|-?", ProgramName);
                Console.Out.WriteLine();
                Console.Out.WriteLine("Dumps all generic events in the CTF data found in the files specified");
                Console.Out.WriteLine("by the given <CTF PATH>(s). Optionally outputs the data to the");
                Console.Out.WriteLine("specified file.");
                Console.Out.WriteLine();
                Console.Out.WriteLine("    -e extension_dir     The directory from which to load extensions.");
                Console.Out.WriteLine("                         Defaults to the working directory.");
                Console.Out.WriteLine();
                Console.Out.WriteLine("    -o out_file          File into which to dump the data. Defaults to stdout.");
                Console.Out.WriteLine("                         If out_file exists, then specify -f to overwrite. By");
                Console.Out.WriteLine("                         default, out_file will be appended if it exists.");
                Console.Out.WriteLine();
                Console.Out.WriteLine("    -f                   If specified with -o, and out_file exists, then");
                Console.Out.WriteLine("                         out_file will be overwritten.");
                Console.Out.WriteLine();
                Console.Out.WriteLine("    -h,-help,-?          Displays this help message and exits.");
                Console.Out.WriteLine();
            }

            private static void EmitError(string message)
            {
                Console.Error.WriteLine("[{0}]: {1}", ProgramName, message);
            }

            private static void EmitError(string fmt, params object[] args)
            {
                var message = string.Format(CultureInfo.CurrentCulture, fmt, args);
                EmitError(message);
            }
        }
    }
}
