// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System;
using Microsoft.Performance.SDK;
using Microsoft.Performance.SDK.Processing;

namespace PerfettoCds.CollectionAccessProviders
{
        public class ArraySegmentAccessProvider<T>
            : ICollectionAccessProvider<ArraySegment<T>, T>
        {
            public bool IsNull(ArraySegment<T> value)
            {
                return value.Count == 0;
            }

            public int GetCount(ArraySegment<T> collection)
            {
                return collection.Count;
            }

            public bool HasUniqueStart => false;

            public bool Equals(ArraySegment<T> x, ArraySegment<T> y)
            {
                if (x.Offset != y.Offset || x.Count != y.Count)
                {
                    return false;
                }

                for (int i = 0; i < x.Count; i++)
                {
                    if (!x[i].Equals(y[i]))
                    {
                        return false;
                    }
                }

                return true;
            }

            public int GetHashCode(ArraySegment<T> collection)
            {
                int hashCode = 42;

                foreach (T val in collection)
                {
                    hashCode = HashCodeUtils.CombineHashCodeValues(hashCode, val.GetHashCode());
                }

                return hashCode;
            }

            public ArraySegment<T> GetParent(ArraySegment<T> collection)
            {
                if (collection.Count == 0)
                {
                    return ArraySegment<T>.Empty;
                }

                return new ArraySegment<T>(collection.Array ?? Array.Empty<T>(), 0, collection.Count - 1);
            }

            public T GetValue(ArraySegment<T> collection, int index)
            {
                if (index >= collection.Count)
                {
                    return PastEndValue;
                }

                return collection[index];
            }

            public T PastEndValue => default;
        }
}