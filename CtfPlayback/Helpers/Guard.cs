// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CtfPlayback.Helpers
{
    /// <summary>
    ///     This class provides for easy declaration of guard clauses in methods.
    ///     <para/>
    ///     Many of the these methods take a `message` and additional parameters. When
    ///     calling an overload that takes `message` and additional values, a token expansion
    ///     will occur. This lets the user place standard information into the message without
    ///     having to copy the values being passed to the method into the message. This reduces
    ///     duplication of effort and ensures that the message stays consistent with the other
    ///     values being utilized for the exception. Each method will document the tokens that
    ///     it supports.
    /// </summary>
    internal static class Guard
    {
        /// <summary>
        ///     Throws an exception if the given value is <c>null</c>.
        /// </summary>
        /// <param name="value">
        ///     The value to check.
        /// </param>
        /// <param name="paramName">
        ///     The name of the parameter to emit in the <see cref="ArgumentNullException"/>
        /// </param>
        /// <exception cref="System.ArgumentNullException">
        ///     <paramref name="value"/> is <c>null</c>.
        /// </exception>
        public static void NotNull(object value, string paramName)
        {
            if (value is null)
            {
                throw new ArgumentNullException(paramName);
            }
        }

        /// <summary>
        ///     Throws an exception if the given value is <c>null</c> or
        ///     composed of exclusivly whitespace characters.
        /// </summary>
        /// <param name="value">
        ///     The value to check.
        /// </param>
        /// <param name="paramName">
        ///     The name of the parameter to emit in the <see cref="ArgumentException"/>
        ///     or <see cref="ArgumentNullException"/>
        /// </param>
        /// <exception cref="System.ArgumentException">
        ///     <paramref name="value"/> is whitespace.
        /// </exception>
        /// <exception cref="System.ArgumentNullException">
        ///     <paramref name="value"/> is <c>null</c>.
        /// </exception>
        public static void NotNullOrWhiteSpace(string value, string paramName)
        {
            Guard.NotNull(value, paramName);
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "Argument cannot be whitespace.",
                    paramName);
            }
        }

        /// <summary>
        ///     Throws an exception is <paramref name="value"/> is equivalent
        ///     to the default value of <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">
        ///     The <see cref="Type"/> of object being checked.
        /// </typeparam>
        /// <param name="value">
        ///     The value to check.
        /// </param>
        /// <param name="paramName">
        ///     The parameter name to emit in the <see cref="ArgumentException"/>.
        /// </param>
        /// <exception cref="System.ArgumentException">
        ///     <paramref name="value"/> is equivalent to the default value of
        ///     <see cref="Type"/> <typeparamref name="T"/>.
        /// </exception>
        public static void NotDefault<T>(T value, string paramName)
            where T : struct
        {
            if (Equals(value, default(T)))
            {
                throw new ArgumentException(
                    "Argument cannot be the default value.",
                    paramName);
            }
        }

        /// <summary>
        ///     Throws an exception if the given condition is false.
        /// </summary>
        /// <param name="condition">
        ///     The condition to check.
        /// </param>
        /// <exception cref="System.ArgumentException">
        ///     <paramref name="condition"/> is <c>false</c>.
        /// </exception>
        public static void IsTrue(bool condition)
        {
            IsTrue(condition, "The specified condition is not valid for this operation.");
        }

        /// <summary>
        ///     Throws an exception if the given condition is false.
        /// </summary>
        /// <param name="condition">
        ///     The condition to check.
        /// </param>
        /// <param name="message">
        ///     A message to emit in the exception.
        /// </param>
        /// <exception cref="System.ArgumentException">
        ///     <paramref name="condition"/> is <c>false</c>.
        /// </exception>
        public static void IsTrue(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        /// <summary>
        ///     Throws an exception if the given condition is true.
        /// </summary>
        /// <param name="condition">
        ///     The condition to check.
        /// </param>
        /// <exception cref="System.ArgumentException">
        ///     <paramref name="condition"/> is <c>true</c>.
        /// </exception>
        public static void IsFalse(bool condition)
        {
            IsFalse(condition, "The specified condition is not valid for this operation.");
        }

        /// <summary>
        ///     Throws an exception if the given condition is true.
        /// </summary>
        /// <param name="condition">
        ///     The condition to check.
        /// </param>
        /// <param name="message">
        ///     A message to emit in the exception.
        /// </param>
        /// <exception cref="System.ArgumentException">
        ///     <paramref name="condition"/> is <c>true</c>.
        /// </exception>
        public static void IsFalse(bool condition, string message)
        {
            if (condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        /// <summary>
        ///     Throws an exception if <paramref name="candidate"/> is not greater
        ///     than the <paramref name="compareValue"/>.
        /// </summary>
        /// <typeparam name="T">
        ///     The <see cref="Type"/> of values being compared.
        /// </typeparam>
        /// <param name="candidate">
        ///     The value to compare.
        /// </param>
        /// <param name="compareValue">
        ///     The value against which <paramref name="candidate"/> is to be compared.
        /// </param>
        /// <param name="paramName">
        ///     The parameter name to emit in the <see cref="ArgumentException"/>.
        /// </param>
        /// <exception cref="System.ArgumentOutOfRangeException">
        ///     <paramref name="candidate"/> is not greater than <paramref name="compareValue"/>.
        /// </exception>
        public static void GreaterThan<T>(T candidate, T compareValue, string paramName)
             where T : IComparable<T>
        {
            GreaterThan(candidate, compareValue, paramName, "The provided value must be greater than the compare value.");
        }

        /// <summary>
        ///     Throws an exception if <paramref name="candidate"/> is not greater
        ///     than the <paramref name="compareValue"/>.
        ///     <para/>
        ///     Tokens supported:
        ///     {paramName} - <paramref name="paramName"/> value
        ///     {compareValue} - <paramref name="compareValue"/> value
        /// </summary>
        /// <typeparam name="T">
        ///     The <see cref="Type"/> of values being compared.
        /// </typeparam>
        /// <param name="candidate">
        ///     The value to compare.
        /// </param>
        /// <param name="compareValue">
        ///     The value against which <paramref name="candidate"/> is to be compared.
        /// </param>
        /// <param name="paramName">
        ///     The parameter name to emit in the <see cref="ArgumentException"/>.
        /// </param>
        /// <param name="message">
        ///     A message to emit in the exception.
        /// </param>
        /// <exception cref="System.ArgumentOutOfRangeException">
        ///     <paramref name="candidate"/> is not greater than <paramref name="compareValue"/>.
        /// </exception>
        public static void GreaterThan<T>(T candidate, T compareValue, string paramName, string message)
             where T : IComparable<T>
        {
            if (candidate.CompareToSafe(compareValue) <= 0)
            {
                var formattedMessage = FormatTokens(
                    message,
                    new KeyValuePair<string, string>("{paramName}", paramName),
                    new KeyValuePair<string, string>("{compareValue}", compareValue?.ToString()));
                throw new ArgumentOutOfRangeException(paramName, candidate, formattedMessage);
            }
        }

        /// <summary>
        ///     Throws an exception if <paramref name="candidate"/> is not greater
        ///     than or equal to the <paramref name="compareValue"/>.
        /// </summary>
        /// <typeparam name="T">
        ///     The <see cref="Type"/> of values being compared.
        /// </typeparam>
        /// <param name="candidate">
        ///     The value to compare.
        /// </param>
        /// <param name="compareValue">
        ///     The value against which <paramref name="candidate"/> is to be compared.
        /// </param>
        /// <param name="paramName">
        ///     The parameter name to emit in the <see cref="ArgumentException"/>.
        /// </param>
        /// <exception cref="System.ArgumentOutOfRangeException">
        ///     <paramref name="candidate"/> is not greater than or equal to
        ///     <paramref name="compareValue"/>.
        /// </exception>
        public static void GreaterThanOrEqualTo<T>(T candidate, T compareValue, string paramName)
             where T : IComparable<T>
        {
            GreaterThanOrEqualTo(candidate, compareValue, paramName, "The provided value must be greater than or equal to the compare value.");
        }

        /// <summary>
        ///     Throws an exception if <paramref name="candidate"/> is not greater
        ///     than or equal to the <paramref name="compareValue"/>.
        ///     <para/>
        ///     Tokens supported:
        ///     {paramName} - <paramref name="paramName"/> value
        ///     {compareValue} - <paramref name="compareValue"/> value
        /// </summary>
        /// <typeparam name="T">
        ///     The <see cref="Type"/> of values being compared.
        /// </typeparam>
        /// <param name="candidate">
        ///     The value to compare.
        /// </param>
        /// <param name="compareValue">
        ///     The value against which <paramref name="candidate"/> is to be compared.
        /// </param>
        /// <param name="paramName">
        ///     The parameter name to emit in the <see cref="ArgumentException"/>.
        /// </param>
        /// <param name="message">
        ///     A message to emit in the exception.
        /// </param>
        /// <exception cref="System.ArgumentOutOfRangeException">
        ///     <paramref name="candidate"/> is not greater than or equal to
        ///     <paramref name="compareValue"/>.
        /// </exception>
        public static void GreaterThanOrEqualTo<T>(T candidate, T compareValue, string paramName, string message)
             where T : IComparable<T>
        {
            if (candidate.CompareToSafe(compareValue) < 0)
            {
                var formattedMessage = FormatTokens(
                    message,
                    new KeyValuePair<string, string>("{paramName}", paramName),
                    new KeyValuePair<string, string>("{compareValue}", compareValue?.ToString()));
                throw new ArgumentOutOfRangeException(paramName, candidate, formattedMessage);
            }
        }

        /// <summary>
        ///     Throws an exception if <paramref name="candidate"/> is not less
        ///     than the <paramref name="compareValue"/>.
        /// </summary>
        /// <typeparam name="T">
        ///     The <see cref="Type"/> of values being compared.
        /// </typeparam>
        /// <param name="candidate">
        ///     The value to compare.
        /// </param>
        /// <param name="compareValue">
        ///     The value against which <paramref name="candidate"/> is to be compared.
        /// </param>
        /// <param name="paramName">
        ///     The parameter name to emit in the <see cref="ArgumentException"/>.
        /// </param>
        /// <exception cref="System.ArgumentOutOfRangeException">
        ///     <paramref name="candidate"/> is not less than <paramref name="compareValue"/>.
        /// </exception>
        public static void LessThan<T>(T candidate, T compareValue, string paramName)
            where T : IComparable<T>
        {
            LessThan(candidate, compareValue, paramName, "The provided value must be less than the compare value.");
        }

        /// <summary>
        ///     Throws an exception if <paramref name="candidate"/> is not less
        ///     than the <paramref name="compareValue"/>.
        ///     <para/>
        ///     Tokens supported:
        ///     {paramName} - <paramref name="paramName"/> value
        ///     {compareValue} - <paramref name="compareValue"/> value
        /// </summary>
        /// <typeparam name="T">
        ///     The <see cref="Type"/> of values being compared.
        /// </typeparam>
        /// <param name="candidate">
        ///     The value to compare.
        /// </param>
        /// <param name="compareValue">
        ///     The value against which <paramref name="candidate"/> is to be compared.
        /// </param>
        /// <param name="paramName">
        ///     The parameter name to emit in the <see cref="ArgumentException"/>.
        /// </param>
        /// <param name="message">
        ///     A message to emit in the exception.
        /// </param>
        /// <exception cref="System.ArgumentOutOfRangeException">
        ///     <paramref name="candidate"/> is not less than <paramref name="compareValue"/>.
        /// </exception>
        public static void LessThan<T>(T candidate, T compareValue, string paramName, string message)
            where T : IComparable<T>
        {
            if (candidate.CompareToSafe(compareValue) >= 0)
            {
                var formattedMessage = FormatTokens(
                    message,
                    new KeyValuePair<string, string>("{paramName}", paramName),
                    new KeyValuePair<string, string>("{compareValue}", compareValue?.ToString()));
                throw new ArgumentOutOfRangeException(paramName, candidate, formattedMessage);
            }
        }

        /// <summary>
        ///     Throws an exception if <paramref name="candidate"/> is not less
        ///     than or equal to the <paramref name="compareValue"/>.
        /// </summary>
        /// <typeparam name="T">
        ///     The <see cref="Type"/> of values being compared.
        /// </typeparam>
        /// <param name="candidate">
        ///     The value to compare.
        /// </param>
        /// <param name="compareValue">
        ///     The value against which <paramref name="candidate"/> is to be compared.
        /// </param>
        /// <param name="paramName">
        ///     The parameter name to emit in the <see cref="ArgumentException"/>.
        /// </param>
        /// <exception cref="System.ArgumentOutOfRangeException">
        ///     <paramref name="candidate"/> is not less than or equal to <paramref name="compareValue"/>.
        /// </exception>
        public static void LessThanOrEqualTo<T>(T candidate, T compareValue, string paramName)
            where T : IComparable<T>
        {
            LessThanOrEqualTo(candidate, compareValue, paramName, "The provided value must be less than or equal to the compare value.");
        }

        /// <summary>
        ///     Throws an exception if <paramref name="candidate"/> is not less
        ///     than or equal to the <paramref name="compareValue"/>.
        ///     <para/>
        ///     Tokens supported:
        ///     {paramName} - <paramref name="paramName"/> value
        ///     {compareValue} - <paramref name="compareValue"/> value
        /// </summary>
        /// <typeparam name="T">
        ///     The <see cref="Type"/> of values being compared.
        /// </typeparam>
        /// <param name="candidate">
        ///     The value to compare.
        /// </param>
        /// <param name="compareValue">
        ///     The value against which <paramref name="candidate"/> is to be compared.
        /// </param>
        /// <param name="paramName">
        ///     The parameter name to emit in the <see cref="ArgumentException"/>.
        /// </param>
        /// <param name="message">
        ///     A message to emit in the exception.
        /// </param>
        /// <exception cref="System.ArgumentOutOfRangeException">
        ///     <paramref name="candidate"/> is not less than or equal to
        ///     <paramref name="compareValue"/>.
        /// </exception>
        public static void LessThanOrEqualTo<T>(T candidate, T compareValue, string paramName, string message)
             where T : IComparable<T>
        {
            if (candidate.CompareToSafe(compareValue) > 0)
            {
                var formattedMessage = FormatTokens(
                    message,
                    new KeyValuePair<string, string>("{paramName}", paramName),
                    new KeyValuePair<string, string>("{compareValue}", compareValue?.ToString()));
                throw new ArgumentOutOfRangeException(paramName, candidate, formattedMessage);
            }
        }

        /// <summary>
        ///     Makes sure that none of the items in a collection satisfy the given 
        ///     predicate.
        ///     <para/>
        ///     The empty collection vacuosly satisfies this condition.
        /// </summary>
        /// <typeparam name="T">
        ///     The <see cref="Type"/> of items in <paramref name="source"/>.
        /// </typeparam>
        /// <param name="source">
        ///     The collection to validate.
        /// </param>
        /// <param name="predicate">
        ///     The predicate which must not be satisfied by any element in the
        ///     collection.
        /// </param>
        /// <param name="paramName">
        ///     The parameter name to emit in the exception.
        /// </param>
        /// <exception cref="System.ArgumentException">
        ///     One or more elements in <paramref name="source"/> satisfies
        ///     <paramref name="predicate"/>.
        /// </exception>
        public static void None<T>(IEnumerable<T> source, Func<T, bool> predicate, string paramName)
        {
            None(source, predicate, paramName, "One or more items satisfies the specified condition.");
        }

        /// <summary>
        ///     Makes sure that none of the items in a collection satisfy the given 
        ///     predicate.
        ///     <para/>
        ///     The empty collection vacuosly satisfies this condition.
        ///     <para/>
        ///     Tokens supported:
        ///     {paramName} - <paramref name="paramName"/> value
        /// </summary>
        /// <typeparam name="T">
        ///     The <see cref="Type"/> of items in <paramref name="source"/>.
        /// </typeparam>
        /// <param name="source">
        ///     The collection to validate.
        /// </param>
        /// <param name="predicate">
        ///     The predicate which must not be satisfied by any element in the
        ///     collection.
        /// </param>
        /// <param name="paramName">
        ///     The parameter name to emit in the exception.
        /// </param>
        /// <param name="message">
        ///     A message to emit in the exception.
        /// </param>
        /// <exception cref="System.ArgumentException">
        ///     One or more elements in <paramref name="source"/> satisfies
        ///     <paramref name="predicate"/>.
        /// </exception>
        public static void None<T>(IEnumerable<T> source, Func<T, bool> predicate, string paramName, string message)
        {
            NotNull(source, paramName);
            NotNull(predicate, nameof(predicate));
            if (source.Any(predicate))
            {
                var formattedMessage = FormatTokens(
                   message,
                   new KeyValuePair<string, string>("{paramName}", paramName));
                throw new ArgumentException(formattedMessage, paramName);
            }
        }

        /// <summary>
        ///     Makes sure that the collection has at least one element.
        /// </summary>
        /// <typeparam name="T">
        ///     The <see cref="Type"/> of items in <paramref name="source"/>.
        /// </typeparam>
        /// <param name="source">
        ///     The collection to validate.
        /// </param>
        /// <param name="paramName">
        ///     The parameter name to emit in the exception.
        /// </param>
        /// <exception cref="System.ArgumentException">
        ///     <paramref name="source"/> has no elements.
        /// </exception>
        public static void Any<T>(IEnumerable<T> source, string paramName)
        {
            NotNull(source, paramName);
            if (!source.Any())
            {
                throw new ArgumentException("The collection must contain at least one item.", paramName);
            }
        }

        /// <summary>
        ///     Makes sure that at least one of the elements in a collection satisfies the given 
        ///     predicate.
        /// </summary>
        /// <typeparam name="T">
        ///     The <see cref="Type"/> of items in <paramref name="source"/>.
        /// </typeparam>
        /// <param name="source">
        ///     The collection to validate.
        /// </param>
        /// <param name="predicate">
        ///     The predicate which must be satisfied by at least one element in the
        ///     collection.
        /// </param>
        /// <param name="paramName">
        ///     The parameter name to emit in the exception.
        /// </param>
        /// <exception cref="System.ArgumentException">
        ///     None of the elements in <paramref name="source"/> satisfy
        ///     <paramref name="predicate"/>.
        /// </exception>
        public static void Any<T>(IEnumerable<T> source, Func<T, bool> predicate, string paramName)
        {
            Any(source, predicate, paramName, "At least one item must meet the specified condition.");
        }

        /// <summary>
        ///     Makes sure that at least one of the elements in a collection satisfies the given 
        ///     predicate.
        ///     <para/>
        ///     Tokens supported:
        ///     {paramName} - <paramref name="paramName"/> value
        /// </summary>
        /// <typeparam name="T">
        ///     The <see cref="Type"/> of items in <paramref name="source"/>.
        /// </typeparam>
        /// <param name="source">
        ///     The collection to validate.
        /// </param>
        /// <param name="predicate">
        ///     The predicate which must be satisfied by at least one element in the
        ///     collection.
        /// </param>
        /// <param name="paramName">
        ///     The parameter name to emit in the exception.
        /// </param>
        /// <param name="message">
        ///     A message to emit in the exception.
        /// </param>
        /// <exception cref="System.ArgumentException">
        ///     None of the elements in <paramref name="source"/> satisfy
        ///     <paramref name="predicate"/>.
        /// </exception>
        public static void Any<T>(IEnumerable<T> source, Func<T, bool> predicate, string paramName, string message)
        {
            NotNull(source, paramName);
            NotNull(predicate, nameof(predicate));
            if (!source.Any(predicate))
            {
                var formattedMessage = FormatTokens(
                    message,
                    new KeyValuePair<string, string>("{paramName}", paramName));
                throw new ArgumentException(formattedMessage, paramName);
            }
        }

        /// <summary>
        ///     Makes sure that all of the items in a collection satisfy the given 
        ///     predicate.
        ///     <para/>
        ///     The empty collection vacuosly satisfies this condition.
        /// </summary>
        /// <typeparam name="T">
        ///     The <see cref="Type"/> of items in <paramref name="source"/>.
        /// </typeparam>
        /// <param name="source">
        ///     The collection to validate.
        /// </param>
        /// <param name="predicate">
        ///     The predicate which must be satisfied by all elements in the
        ///     collection.
        /// </param>
        /// <param name="paramName">
        ///     The parameter name to emit in the exception.
        /// </param>
        /// <exception cref="System.ArgumentException">
        ///     One or more elements in <paramref name="source"/> fails to satisfy
        ///     <paramref name="predicate"/>.
        /// </exception>
        public static void All<T>(IEnumerable<T> source, Func<T, bool> predicate, string paramName)
        {
            All(source, predicate, paramName, "One or more items fails the specified condition.");
        }

        /// <summary>
        ///     Makes sure that all of the items in a collection satisfy the given 
        ///     predicate.
        ///     <para/>
        ///     The empty collection vacuosly satisfies this condition.
        ///     <para/>
        ///     Tokens supported:
        ///     {paramName} - <paramref name="paramName"/> value
        /// </summary>
        /// <typeparam name="T">
        ///     The <see cref="Type"/> of items in <paramref name="source"/>.
        /// </typeparam>
        /// <param name="source">
        ///     The collection to validate.
        /// </param>
        /// <param name="predicate">
        ///     The predicate which must be satisfied by all elements in the
        ///     collection.
        /// </param>
        /// <param name="paramName">
        ///     The parameter name to emit in the exception.
        /// </param>
        /// <param name="message">
        ///     A message to emit in the exception.
        /// </param>
        /// <exception cref="System.ArgumentException">
        ///     One or more elements in <paramref name="source"/> fails to satisfy
        ///     <paramref name="predicate"/>.
        /// </exception>
        public static void All<T>(IEnumerable<T> source, Func<T, bool> predicate, string paramName, string message)
        {
            NotNull(source, paramName);
            NotNull(predicate, nameof(predicate));
            if (!source.All(predicate))
            {
                var formattedMessage = FormatTokens(
                    message,
                    new KeyValuePair<string, string>("{paramName}", paramName));
                throw new ArgumentException(formattedMessage, paramName);
            }
        }

        private static string FormatTokens(
            string message,
            params KeyValuePair<string, string>[] tokens)
        {
            if (string.IsNullOrWhiteSpace(message) ||
                tokens == null ||
                tokens.Length == 0)
            {
                return message;
            }

            const string nullString = "null";
            var replacement = new StringBuilder(message);
            foreach (var token in tokens)
            {
                replacement.Replace(token.Key, token.Value ?? nullString);
            }

            return replacement.ToString();
        }
    }
}