// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace LTTngDataExtensions.SourceDataCookers.Process
{
    public interface IBuilder<out T>
    {
        T Build();
    }
}