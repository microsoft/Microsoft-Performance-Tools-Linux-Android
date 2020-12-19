// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace LttngDriver
{
    public sealed partial class Program
    {
        public static int Main(string[] args)
        {
            try
            {
                var p = new Program(args);
                return p.Run()
                    ? 0
                    : -1;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
                return e.HResult;
            }
        }
    }
}
