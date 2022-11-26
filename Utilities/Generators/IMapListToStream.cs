// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Utilities.Generators
{
    public interface IMapListToStream
    {
        uint[] MapListToStartIndex 
        { 
            get;
        }

        uint[] MapIndexToList 
        { 
            get;
        }
    }

    public interface IMapListToStream<T>
        : IMapListToStream
    {
        int ListSize(uint list);

        T IndexIntoList(uint list, int index);
    }
}