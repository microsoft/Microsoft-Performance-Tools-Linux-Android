// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using CtfPlayback.Metadata.TypeInterfaces;
using LTTngCds.CtfExtensions.DescriptorInterfaces;

namespace LTTngCds.CtfExtensions.Descriptors
{
    /// <summary>
    /// A definition of an event.
    /// </summary>
    internal class EventDescriptor
        : IEventDescriptor
    {
        internal EventDescriptor(
                IReadOnlyDictionary<string, string> assignments,
                IReadOnlyDictionary<string, ICtfTypeDescriptor> typeDeclarations)
        {
            this.Assignments = assignments;
            this.TypeDeclarations = typeDeclarations;

            this.SetId(assignments);
            this.SetStream(assignments);
            this.SetLogLevel(assignments);

            if (!assignments.TryGetValue("name", out var name))
            {
                throw new LTTngMetadataException("event descriptor is missing 'name' property.");
            }
            this.Name = string.Intern(name.Trim(new[] { '"' }));

            this.SetPayload(typeDeclarations);
        }

        public uint Id { get; private set; }

        public string Name { get; private set; }

        public int Stream { get; private set; }

        public uint LogLevel { get; private set; }

        // LTTNG defines no context for an event

        public ICtfTypeDescriptor Context => null;

        public ICtfTypeDescriptor Payload { get; private set; }

        public IReadOnlyDictionary<string, string> Assignments { get; }

        public IReadOnlyDictionary<string, ICtfTypeDescriptor> TypeDeclarations { get; }

        public override string ToString()
        {
            return this.Name;
        }

        private void SetId(IReadOnlyDictionary<string, string> assignments)
        {
            if (!assignments.TryGetValue("id", out var integerString))
            {
                throw new LTTngMetadataException("event descriptor is missing 'id' property.");
            }

            if (!uint.TryParse(integerString, out var id))
            {
                throw new LTTngMetadataException($"event descriptor 'id' cannot be converted into a {this.Id.GetType()}.");
            }

            this.Id = id;
        }

        private void SetStream(IReadOnlyDictionary<string, string> assignments)
        {
            if (!assignments.TryGetValue("stream_id", out var integerString))
            {
                throw new LTTngMetadataException("event descriptor is missing 'stream_id' property.");
            }

            if (!int.TryParse(integerString, out var streamId))
            {
                throw new LTTngMetadataException($"event descriptor 'stream_id' cannot be converted into a {this.Stream.GetType()}.");
            }

            this.Stream = streamId;
        }

        /// <summary>
        /// I don't think this is required, so not throwing if it isn't found.
        /// Still throws if it exists and cannot be converted.
        /// </summary>
        /// <param name="assignments"></param>
        private void SetLogLevel(IReadOnlyDictionary<string, string> assignments)
        {
            if (!assignments.TryGetValue("loglevel", out var integerString))
            {
                return;
            }

            if (!uint.TryParse(integerString, out var logLevel))
            {
                throw new LTTngMetadataException($"event descriptor 'loglevel' cannot be converted into a {this.Stream.GetType()}.");
            }

            this.LogLevel = logLevel;
        }

        private void SetPayload(IReadOnlyDictionary<string, ICtfTypeDescriptor> typeDeclarations)
        {
            if (!typeDeclarations.TryGetValue("fields", out var typeDescriptor))
            {
                throw new LTTngMetadataException("event descriptor is missing 'fields' type assignment.");
            }

            if (!(typeDescriptor is ICtfStructDescriptor payloadStruct))
            {
                throw new LTTngMetadataException("event descriptor type 'fields' is not a structure.");
            }

            this.Payload = payloadStruct;
        }
    }
}