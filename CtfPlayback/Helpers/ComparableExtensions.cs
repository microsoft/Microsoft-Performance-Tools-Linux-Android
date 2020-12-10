// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace CtfPlayback.Helpers
{
    /// <summary>
    ///     Provides static (Shared in Visual Basic) methods for interacting
    ///     with <see cref="IComparable"/> instances.
    /// </summary>
    internal static class ComparableExtensions
    {
        /// <summary>
        ///     Performs the <see cref="IComparable{T}.CompareTo(T)" /> operation,
        ///     handling the case  where any parameter could be null, even the
        ///     source. It is safe to call this method on a null reference.
        /// </summary>
        /// <returns>
        ///     An integer value denoting the ordering of <paramref name="self"/> in
        ///     relation to <paramref name="compareValue"/>. See <see cref="IComparable"/>.
        ///     <para/>
        ///     A positive value indicates that <paramref name="self"/> is
        ///     strictly greater than <paramref name="compareValue"/>.
        ///     <para/>
        ///     A zero value indicates that <paramref name="self"/> is
        ///     equal to <paramref name="compareValue"/>.
        ///     <para/>
        ///     A negative value indicates that <paramref name="self"/> is
        ///     strictly less than <paramref name="compareValue"/>.
        /// </returns>
        public static int CompareToSafe<T>(this T self, T compareValue)
            where T : IComparable<T>
        {
            if (!ReferenceEquals(self, null))
            {
                return self.CompareTo(compareValue);
            }
            else
            {
                if (!ReferenceEquals(compareValue, null))
                {
                    // perform the comparison of the non-null value to null, and
                    // invert the result to get null compared to non-null for type T.
                    // We do this because we do not want to make assumptions about where
                    // the type T places null values in the ordering of its values.
                    var invertedCompareValue = compareValue.CompareTo(self);
                    if (invertedCompareValue == int.MinValue)
                    {
                        // We are avoiding overflow here (-int.MinValue will overflow.)
                        // candidate is < compareValue, so explicitly return a
                        // positive value (because compareValue > candidate, and
                        // candidate.CompareTo(compareValue) would return a positive
                        // value.)
                        return 1;
                    }
                    else
                    {
                        // we can safely invert the comparison value to get
                        // the result of null compared to the compare value
                        // without fear of overflow.
                        return -invertedCompareValue;
                    }
                }
                else
                {
                    // both values are null, and thus are equal.
                    return 0;
                }
            }
        }
    }
}