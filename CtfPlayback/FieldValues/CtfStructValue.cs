// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Text;
using CtfPlayback.Metadata;

namespace CtfPlayback.FieldValues
{
    /// <summary>
    /// Represents a struct field from a CTF event
    /// </summary>
    public class CtfStructValue
        : CtfFieldValue
    {
        private readonly Dictionary<string, CtfFieldValue> fieldsByName = new Dictionary<string, CtfFieldValue>();
        private readonly List<CtfFieldValue> fields = new List<CtfFieldValue>();

        /// <summary>
        /// Constructor
        /// </summary>
        public CtfStructValue()
            : base(CtfTypes.Struct)
        {
        }

        /// <summary>
        /// List of all of the fields.
        /// </summary>
        public IReadOnlyList<CtfFieldValue> Fields => this.fields;

        /// <summary>
        /// The fields, indexed by field name.
        /// </summary>
        public IReadOnlyDictionary<string, CtfFieldValue> FieldsByName => this.fieldsByName;

        /// <summary>
        /// Returns a string representation of the field value.
        /// Useful when text dumping the contents of an event stream.
        /// </summary>
        /// <returns>The field value as a string</returns>
        public override string GetValueAsString(GetValueAsStringOptions options = GetValueAsStringOptions.NoOptions)
        {
            var sb = new StringBuilder("{ ");

            if (this.Fields.Count > 0)
            {
                sb.Append(this.Fields[0].ToString(options));
            }

            for (int x = 1; x < this.Fields.Count; x++)
            {
                sb.Append(", " + this.Fields[x].ToString(options));
            }

            sb.Append(" }");

            return string.Intern(sb.ToString());
        }

        /// <summary>
        /// Interpret the given field as a signed byte
        /// </summary>
        /// <param name="fieldName">Field name</param>
        /// <returns>Field value</returns>
        public sbyte ReadFieldAsInt8(string fieldName)
        {
            return GetSByte(GetFieldAsIntegerValue(fieldName), fieldName);
        }

        /// <summary>
        /// Interpret the given field as a byte
        /// </summary>
        /// <param name="fieldName">Field name</param>
        /// <returns>Field value</returns>
        public byte ReadFieldAsUInt8(string fieldName)
        {
            return GetByte(GetFieldAsIntegerValue(fieldName), fieldName);
        }

        /// <summary>
        /// Interpret the given field as a signed short
        /// </summary>
        /// <param name="fieldName">Field name</param>
        /// <returns>Field value</returns>
        public short ReadFieldAsInt16(string fieldName)
        {
            return GetSByte(GetFieldAsIntegerValue(fieldName), fieldName);
        }

        /// <summary>
        /// Interpret the given field as an unsigned short
        /// </summary>
        /// <param name="fieldName">Field name</param>
        /// <returns>Field value</returns>
        public ushort ReadFieldAsUInt16(string fieldName)
        {
            return GetByte(GetFieldAsIntegerValue(fieldName), fieldName);
        }

        /// <summary>
        /// Interpret the given field as an int
        /// </summary>
        /// <param name="fieldName">Field name</param>
        /// <returns>Field value</returns>
        public int ReadFieldAsInt32(string fieldName)
        {
            return GetInt(GetFieldAsIntegerValue(fieldName), fieldName);
        }

        /// <summary>
        /// Interpret the given field as an unsigned int
        /// </summary>
        /// <param name="fieldName">Field name</param>
        /// <returns>Field value</returns>
        public uint ReadFieldAsUInt32(string fieldName)
        {
            return GetUInt(GetFieldAsIntegerValue(fieldName), fieldName);
        }

        /// <summary>
        /// Interpret the given field as a long
        /// </summary>
        /// <param name="fieldName">Field name</param>
        /// <returns>Field value</returns>
        public long ReadFieldAsInt64(string fieldName)
        {
            return GetLong(GetFieldAsIntegerValue(fieldName), fieldName);
        }

        /// <summary>
        /// Interpret the given field as an unsigned long
        /// </summary>
        /// <param name="fieldName">Field name</param>
        /// <returns>Field value</returns>
        public ulong ReadFieldAsUInt64(string fieldName)
        {
            return GetUInt(GetFieldAsIntegerValue(fieldName), fieldName);
        }

        /// <summary>
        /// Interpret the given field as an array
        /// </summary>
        /// <param name="fieldName">Field name</param>
        /// <returns>Field value</returns>
        public CtfArrayValue ReadFieldAsArray(string fieldName)
        {
            if (!this.FieldsByName.TryGetValue(fieldName, out var fieldValue))
            {
                throw new CtfPlaybackException("Event does not contain {fieldName} field.");
            }

            if (!(fieldValue is CtfArrayValue arrayValue))
            {
                throw new CtfPlaybackException($"{fieldName} field is not an array.");
            }

            return arrayValue;
        }

        /// <summary>
        /// Interpret the given field as a string
        /// </summary>
        /// <param name="fieldName">Field name</param>
        /// <returns>Field value</returns>
        public CtfStringValue ReadFieldAsString(string fieldName)
        {
            if (!this.FieldsByName.TryGetValue(fieldName, out var fieldValue))
            {
                throw new CtfPlaybackException($"Event does not contain {fieldName} field.");
            }

            if (!(fieldValue is CtfStringValue fieldAsString))
            {
                throw new CtfPlaybackException($"{fieldName} field is not a string.");
            }

            return fieldAsString;
        }

        internal override CtfFieldValue FindField(string identifier)
        {
            int separatorIndex = identifier.IndexOf('.');
            if (separatorIndex < 0)
            {
                // search in the current scope
                if (!this.FieldsByName.TryGetValue(identifier, out var field))
                {
                    if (this.Parent == null)
                    {
                        return null;
                    }

                    field = this.Parent.FindField(identifier);
                }

                return field;
            }

            string childScopeName = identifier.Substring(0, separatorIndex);
            string identifierWithinChildScope = identifier.Substring(separatorIndex + 1);

            if (this.FieldsByName.TryGetValue(childScopeName, out var childScope))
            {
                var childField = childScope.FindField(identifierWithinChildScope);
                if (childField != null)
                {
                    return childField;
                }
            }

            return Parent?.FindField(identifier);
        }

        internal bool AddValue(CtfFieldValue value)
        {
            this.fields.Add(value);

            if (this.fieldsByName.ContainsKey(value.FieldName))
            {
                return false;
            }

            this.fieldsByName.Add(value.FieldName, value);

            return true;
        }

        private CtfIntegerValue GetFieldAsIntegerValue(string fieldName)
        {
            if (!this.FieldsByName.TryGetValue(fieldName, out var fieldValue))
            {
                throw new CtfPlaybackException($"Event does not contain {fieldName} field.");
            }

            if (!(fieldValue is CtfIntegerValue fieldAsIntegerValue))
            {
                throw new CtfPlaybackException($"{fieldName} field is not an integer.");
            }

            return fieldAsIntegerValue;
        }

        private byte GetByte(CtfIntegerValue integerValue, string fieldName)
        {
            if (!integerValue.Value.TryGetUInt8(out var field))
            {
                throw new CtfPlaybackException($"{fieldName} cannot be parsed as a byte.");
            }

            return field;
        }

        private sbyte GetSByte(CtfIntegerValue integerValue, string fieldName)
        {
            if (!integerValue.Value.TryGetInt8(out var field))
            {
                throw new CtfPlaybackException($"{fieldName} cannot be parsed as an sbyte.");
            }

            return field;
        }

        private short GetShort(CtfIntegerValue integerValue, string fieldName)
        {
            if (!integerValue.Value.TryGetInt16(out var field))
            {
                throw new CtfPlaybackException($"{fieldName} cannot be parsed as a short.");
            }

            return field;
        }

        private ushort GetUShort(CtfIntegerValue integerValue, string fieldName)
        {
            if (!integerValue.Value.TryGetUInt16(out var field))
            {
                throw new CtfPlaybackException($"{fieldName} cannot be parsed as an ushort.");
            }

            return field;
        }

        private int GetInt(CtfIntegerValue integerValue, string fieldName)
        {
            if (!integerValue.Value.TryGetInt32(out var field))
            {
                throw new CtfPlaybackException($"{fieldName} cannot be parsed as an int.");
            }

            return field;
        }

        private uint GetUInt(CtfIntegerValue integerValue, string fieldName)
        {
            if (!integerValue.Value.TryGetUInt32(out var field))
            {
                throw new CtfPlaybackException($"{fieldName} cannot be parsed as a uint.");
            }

            return field;
        }

        private long GetLong(CtfIntegerValue integerValue, string fieldName)
        {
            if (!integerValue.Value.TryGetInt64(out var field))
            {
                throw new CtfPlaybackException($"{fieldName} cannot be parsed as a long.");
            }

            return field;
        }

        private ulong GetULong(CtfIntegerValue integerValue, string fieldName)
        {
            if (!integerValue.Value.TryGetUInt64(out var field))
            {
                throw new CtfPlaybackException($"{fieldName} cannot be parsed as a ulong.");
            }

            return field;
        }
    }
}