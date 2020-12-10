// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CtfPlayback.Metadata;

namespace CtfPlayback.FieldValues
{
    /// <summary>
    /// Base abstract class for representing a field value of a CTF event.
    /// </summary>
    public abstract class CtfFieldValue
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="fieldType">The type of the field</param>
        protected CtfFieldValue(CtfTypes fieldType)
        {
            this.FieldType = fieldType;
        }

        /// <summary>
        /// The type of the field
        /// </summary>
        public CtfTypes FieldType { get; }

        /// <summary>
        /// The name of the field
        /// </summary>
        public string FieldName { get; set; }

        internal CtfStructValue Parent { get; set; }

        /// <summary>
        /// Returns a string representation of the field value.
        /// Useful when text dumping the contents of an event stream.
        /// </summary>
        /// <returns>The field value as a string</returns>
        public abstract string GetValueAsString(GetValueAsStringOptions options = GetValueAsStringOptions.NoOptions);

        /// <summary>
        /// Returns a string representation of the field, name and value.
        /// </summary>
        /// <returns>The field as a string</returns>
        public override string ToString()
        {
            return this.ToString(GetValueAsStringOptions.NoOptions);
        }

        /// <summary>
        /// Returns a string representation of the field, name and value.
        /// </summary>
        /// <returns>The field as a string</returns>
        public string ToString(GetValueAsStringOptions options)
        {

            string fieldName = FieldName;
            if (options.HasFlag(GetValueAsStringOptions.TrimBeginningUnderscore) && FieldName.StartsWith("_"))
            {
                fieldName = FieldName.Substring(1);
            }

            return string.Intern(fieldName + " = " + this.GetValueAsString(options));
        }

        internal virtual CtfFieldValue FindField(string identifier)
        {
            return null;
        }
    }
}