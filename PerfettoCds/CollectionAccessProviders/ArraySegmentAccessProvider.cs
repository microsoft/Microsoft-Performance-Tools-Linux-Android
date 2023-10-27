// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System;
using Microsoft.Performance.SDK.Processing;

namespace PerfettoCds.CollectionAccessProviders
{
        public class ArraySegmentAccessProvider<T>
            : ICollectionAccessProvider<ArraySegment<T>, T>
        {
            public bool IsNull(ArraySegment<T> collection)
            {
                return collection == null || collection.Count == 0;
            }

            public int GetCount(ArraySegment<T> collection)
            {
                if (collection == null)
                {
                    return 0;
                }

                return collection.Count;
            }

            public bool HasUniqueStart => false;

            public bool Equals(ArraySegment<T> x, ArraySegment<T> y)
            {
                if (x == null || y == null)
                {
                    return x == y;
                }

                return x.Equals(y);
            }

            public int GetHashCode(ArraySegment<T> collection)
            {
                if (collection == null)
                {
                    return 0;
                }

                return collection.GetHashCode();
            }

            public ArraySegment<T> GetParent(ArraySegment<T> collection)
            {
                if (collection == null || collection.Count == 0)
                {
                    return ArraySegment<T>.Empty;
                }

                return new ArraySegment<T>(collection.Array ?? Array.Empty<T>(), 0, collection.Count - 1);
            }

            public T GetValue(ArraySegment<T> collection, int index)
            {
                if (collection == null || index >= collection.Count)
                {
                    return PastEndValue;
                }

                return collection[index];
            }

            public T PastEndValue => default;
        }
}
