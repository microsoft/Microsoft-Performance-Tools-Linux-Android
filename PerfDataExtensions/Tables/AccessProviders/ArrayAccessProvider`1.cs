// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Performance.SDK.Processing;

namespace PerfDataExtensions.Tables.AccessProviders
{
    // This is copied from elsewhere and probably needs to be in the SDK
    public struct ArrayAccessProvider<T>
        : ICollectionAccessProvider<T[], T>
    {
        #region ICollectionAccessProvider<T[],T> Members

        public T GetValue(T[] collection, int index)
        {
            if (index < collection.Length)
            {
                T value = collection[index];
                return value;
            }
            return PastEndValue;
        }

        public T PastEndValue
        {
            get
            {
                return default(T);
            }
        }

        #endregion

        #region ICollectionInfoProvider<T[]> Members

        public bool HasUniqueStart
        {
            get { return false; }
        }

        public int GetCount(T[] collection)
        {
            return collection.Length;
        }

        #endregion

        #region INullableInfoProvider<T[]> Members

        public bool IsNull(T[] value)
        {
            return (value == null);
        }

        #endregion

        public T[] GetParent(T[] collection)
        {
            throw new System.NotImplementedException();
        }

        public bool Equals(T[] x, T[] y)
        {
            throw new System.NotImplementedException();
        }

        public int GetHashCode(T[] x)
        {
            throw new System.NotImplementedException();
        }
    }
}