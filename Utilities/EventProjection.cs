// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System;
using Microsoft.Performance.SDK.Processing;

namespace Utilities
{
    public struct EventProjection<T> : IProjection<int, T>
    {
        private readonly ProcessedEventData<T> events;

        public EventProjection(ProcessedEventData<T> events)
        {
            this.events = events;
        }

        public Type SourceType => typeof(int);

        public Type ResultType => typeof(T);

        public T this[int value] => this.events[(uint)value];
    }
}
