// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CtfPlayback.Metadata.TypeInterfaces;
using CtfPlayback.Metadata.Types;

namespace CtfPlayback.Metadata.InternalHelpers
{
    /// <summary>
    /// A simple class to make parsing out properties easier.
    /// </summary>
    internal class CtfPropertyBag
    {
        private readonly Dictionary<string, string> properties = new Dictionary<string, string>();
        private readonly Dictionary<string, ICtfTypeDescriptor> typeProperties = new Dictionary<string, ICtfTypeDescriptor>();

        internal IReadOnlyDictionary<string, string> Assignments => this.properties;

        internal void Clear()
        {
            this.properties.Clear();
            this.typeProperties.Clear();
        }

        internal bool GetBoolean(string name)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(name));

            string value = this.properties[name];

            switch (value)
            {
                case "0":
                    return false;

                case "1":
                    return true;

                default:
                    return bool.Parse(value);
            }
        }

        internal bool TryGetBoolean(string key, out bool value)
        {
            value = false;

            if (!this.properties.TryGetValue(key, out var boolString))
            {
                return false;
            }

            switch (boolString)
            {
                case "0":
                    value = false;
                    return true;

                case "1":
                    value = true;
                    return true;

                default:
                    return bool.TryParse(boolString, out value);
            }
        }

        internal bool ContainsKey(string key)
        {
            return this.properties.ContainsKey(key);
        }

        internal short GetShort(string name)
        {
            string value = this.properties[name];
            return short.Parse(value);
        }

        internal short? GetShortOrNull(string name)
        {
            string value;
            this.properties.TryGetValue(name, out value);

            if (value == null)
            {
                return null;
            }

            switch (value)
            {
                case "decimal":
                    return 10;
                case "hexadecimal":
                    return 16;
                case "binary":
                    return 2;
                default:
                    return short.Parse(value);
            }
        }

        internal string GetString(string name)
        {
            string value;
            this.properties.TryGetValue(name, out value);
            return value;
        }

        internal bool TryGetString(string key, out string value)
        {
            return this.properties.TryGetValue(key, out value);
        }

        internal int? GetIntOrNull(string name)
        {
            string value;
            this.properties.TryGetValue(name, out value);

            if (value == null)
            {
                return null;
            }

            return int.Parse(value);
        }

        internal int GetInt(string name)
        {
            return int.Parse(this.properties[name]);
        }

        internal ulong GetUlong(string name)
        {
            return ulong.Parse(this.properties[name]);
        }

        internal ICtfTypeDescriptor GetType(string name)
        {
            this.typeProperties.TryGetValue(name, out var value);
            return value;
        }

        internal ICtfStructDescriptor GetStruct(string name)
        {
            this.typeProperties.TryGetValue(name, out var value);
            return value as ICtfStructDescriptor;
        }

        internal void AddValue(string name, string value)
        {
            this.properties.Add(name, value);
        }

        internal void AddValue(string name, CtfMetadataTypeDescriptor value)
        {
            this.typeProperties[name] = value;
        }

        internal uint GetUInt(string name)
        {
            return this.properties.TryGetValue(name, out string value) && 
                   uint.TryParse(value, out uint num) ? num : 0;            
        }

        internal bool Any()
        {
            return this.properties.Any() || this.typeProperties.Any();
        }

        internal string GetByteOrder()
        {
            if (!this.TryGetString("byte_order", out string byteOrder))
            {
                // this is the default value according to specification 1.82 section 4.1.3.
                // native defers to the byte order specified in the trace description
                //
                byteOrder = "native";
            }

            return byteOrder;
        }
    }
}