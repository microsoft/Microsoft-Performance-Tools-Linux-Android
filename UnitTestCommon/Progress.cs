// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace UnitTestCommon
{
    public class Progress : IProgress<int>
    {
        public void Report(int progress)
        {
            Console.WriteLine($"Progress: {progress}");
        }
    }
}
