// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Performance.SDK;
using Microsoft.Performance.SDK.Processing;

namespace PerfDataExtensions.Tables.Generators
{
    public static class SequentialGenerator
    {
        public static IProjection<int, T> Create<T>(int countFirst, IProjection<int, T> first, IProjection<int, T> second)
        {
            var typeArgs = new[] { typeof(T), first.GetType(), second.GetType(), };
            var constructorArgs = new object[] { countFirst, first, second, };
            return Instantiate<IProjection<int, T>>(typeof(SequentialProjection<,,>), typeArgs, constructorArgs);
        }

        public static IProjection<int, T> Create<T>(int countFirst, IProjection<int, T> first, int countSecond, IProjection<int, T> second, IProjection<int, T> third)
        {
            return SequentialGenerator.Create<T>(
                countFirst + countSecond,
                SequentialGenerator.Create<T>(countFirst, first, second),
                third);
        }

        public static IProjection<int, T> Create<T, TList>(TList[] stridedData)
            where TList : IList<T>
        {
            return Create<T, TList>(stridedData, null);
        }

        public static IProjection<int, T> Create<T, TList>(TList[] stridedData, out IMapListToStream<T> stream)
            where TList : IList<T>
        {
            var generator = Create<T, TList>(stridedData, null);
            stream = (IMapListToStream<T>)generator;
            return generator;
        }

        public static IProjection<int, T> Create<T, TList>(TList[] stridedData, int[] sizeToUse)
            where TList : IList<T>
        {
            var typeArgs = new[] { typeof(T), typeof(TList), };
            var constructorArgs = new object[] { stridedData, sizeToUse, };
            return Instantiate<IProjection<int, T>>(typeof(SequentialProjection<,>), typeArgs, constructorArgs);
        }

        public static IProjection<int, T> CreateFromOffset<T>(IProjection<int, T> projection, int offset)
        {
            if (!(projection is IMapListToStream))
            {
                throw new InvalidOperationException("The projection being based in must have been constructed using SequentialColumn.CreateComputedColumn");
            }

            var typeArgs = new[] { typeof(T), projection.GetType(), };
            var constructorArgs = new object[] { projection, offset, };
            return Instantiate<IProjection<int, T>>(typeof(SequentialProjectionFromOffset<,>), typeArgs, constructorArgs);
        }

        public static IProjection<int, T> Create<T>(
            params Tuple<int, IProjection<int, T>>[] projections)
        {
            Guard.NotNull(projections, nameof(projections));
            Guard.Any(projections, nameof(projections));
            Guard.All(
                projections,
                f => f != null && f.Item1 >= 0 && f.Item2 != null,
                nameof(projections));

            if (projections.Length == 1)
            {
                return projections[0].Item2;
            }

            if (projections.Length == 2)
            {
                var generator = new SequentialProjection<T, IProjection<int, T>, IProjection<int, T>>(
                    projections[0].Item1,
                    projections[0].Item2,
                    projections[1].Item2);
                return generator;
            }

            var finalCount = projections[0].Item1;
            var finalProjection = projections[0].Item2;
            for (var i = 1; i < projections.Length; ++i)
            {
                finalProjection = new SequentialProjection<T, IProjection<int, T>, IProjection<int, T>>(
                    finalCount,
                    finalProjection,
                    projections[i].Item2);
                finalCount += projections[i].Item1;
            }

            return finalProjection;
        }

        private static T Instantiate<T>(Type generic, Type[] typeArgs, params object[] args)
        {
            var genericType = generic.MakeGenericType(typeArgs);
            var instance = Activator.CreateInstance(genericType, args);
            return (T)instance;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        private struct SequentialProjection<T, TProjection1, TProjection2>
            : IProjection<int, T>
              // IRequiresSymbolDataContext
              where TProjection1 : IProjection<int, T>
              where TProjection2 : IProjection<int, T>
        {
            private int countFirst;
            private TProjection1 generatorFirst;
            private TProjection2 generatorSecond;

            public SequentialProjection(int countFirst, TProjection1 generatorFirst, TProjection2 generatorSecond)
            {
                this.countFirst = countFirst;
                this.generatorFirst = generatorFirst;
                this.generatorSecond = generatorSecond;
            }

            public T this[int value]
            {
                get
                {
                    if (value < this.countFirst)
                    {
                        return this.generatorFirst[value];
                    }
                    else
                    {
                        return this.generatorSecond[value - countFirst];
                    }
                }
            }

            public Type SourceType
            {
                get { return typeof(int); }
            }

            ////public DataContext DataContext
            ////{
            ////    get
            ////    {
            ////        return RequiresSymbolDataContext.GetDataContextIfNecessary(this.generatorFirst);
            ////    }
            ////    set
            ////    {
            ////        RequiresSymbolDataContext.SetDataContextIfNecessary(this.generatorFirst, value);
            ////        RequiresSymbolDataContext.SetDataContextIfNecessary(this.generatorSecond, value);
            ////    }
            ////}

            ////public bool RequiresSymbols
            ////{
            ////    get
            ////    {
            ////        return RequiresSymbolDataContext.DoesRequireSymbols(this.generatorFirst) ||
            ////               RequiresSymbolDataContext.DoesRequireSymbols(this.generatorSecond);
            ////    }
            ////}

            public Type ResultType
            {
                get { return typeof(T); }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        private struct SequentialProjection<T, TList>
            : IProjection<int, T>,
              IMapListToStream<T>
              where TList : IList<T>
        {
            private int[] listSize;
            private uint[] mapListToStartIndex;
            private uint[] mapIndexToList;
            private TList[] generatorList;

            public SequentialProjection(TList[] generatorList, int[] sizeToUse)
            {
                this.generatorList = generatorList;
                this.listSize = new int[generatorList.Length];
                for (int index = 0; index < generatorList.Length; ++index)
                {
                    listSize[index] = generatorList[index].Count;
                }

                int[] initSize = sizeToUse ?? this.listSize;
                InitializeMapToIndexList(generatorList, initSize, out mapIndexToList, out mapListToStartIndex);

            }

            private static void InitializeMapToIndexList(TList[] generatorList, int[] sizeToUse, out uint[] result, out uint[] startIndices)
            {
                uint count = 0;
                startIndices = new uint[generatorList.Length];
                {
                    for (int listIndex = 0; listIndex < generatorList.Length; ++listIndex)
                    {
                        startIndices[listIndex] = count;
                        count += (uint)sizeToUse[listIndex];
                    }
                }

                result = new uint[count];
                {
                    uint currentIndex = 0;
                    for (int listIndex = 0; listIndex < generatorList.Length; ++listIndex)
                    {
                        TList list = generatorList[listIndex];
                        long currentCount = sizeToUse[listIndex];
                        for (int currentListIndex = 0; currentListIndex < currentCount; ++currentListIndex)
                        {
                            result[currentIndex] = (uint)listIndex;
                            ++currentIndex;
                        }
                    }
                }
            }

            public T this[int value]
            {
                get
                {
                    uint listIndex = this.mapIndexToList[value];
                    uint relativeOffset = (uint)value - this.mapListToStartIndex[listIndex];
                    return this.generatorList[listIndex][(int)relativeOffset];
                }
            }

            public Type SourceType
            {
                get { return typeof(int); }
            }

            public Type ResultType
            {
                get { return typeof(T); }
            }

            public uint[] MapIndexToList
            {
                get
                {
                    return this.mapIndexToList;
                }
            }

            public uint[] MapListToStartIndex
            {
                get
                {
                    return this.mapListToStartIndex;
                }
            }

            public int ListSize(uint list)
            {
                return this.listSize[list];
            }

            public T IndexIntoList(uint list, int index)
            {
                return this.generatorList[list][index];
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        private struct SequentialProjectionFromOffset<T, TProjection>
            : IProjection<int, T>
              where TProjection : IProjection<int, T>, IMapListToStream<T>
        {
            private int offset;
            private TProjection generator;

            public SequentialProjectionFromOffset(TProjection generator, int offset)
            {
                this.generator = generator;
                this.offset = offset;
            }

            public T this[int value]
            {
                get
                {
                    uint listIndex = this.generator.MapIndexToList[value];
                    int sublistIndex = value - (int)this.generator.MapListToStartIndex[listIndex];
                    int sublistIndexToUse = sublistIndex + this.offset;
                    int sublistCount = this.generator.ListSize(listIndex);

                    if (sublistIndexToUse < 0 || sublistIndexToUse >= sublistCount)
                    {
                        return default(T);
                    }

                    return this.generator.IndexIntoList(listIndex, sublistIndexToUse);
                }
            }

            public Type SourceType
            {
                get { return typeof(int); }
            }

            public Type ResultType
            {
                get { return typeof(T); }
            }
        }
    }
}
