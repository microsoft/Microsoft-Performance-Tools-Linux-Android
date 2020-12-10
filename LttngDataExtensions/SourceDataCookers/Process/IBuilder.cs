// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace LttngDataExtensions.SourceDataCookers.Process
{
    public interface IBuilder<out T>
    {
        T Build();
    }
}