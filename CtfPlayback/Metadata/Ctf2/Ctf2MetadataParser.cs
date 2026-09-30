// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CtfPlayback.Metadata.Helpers;
using CtfPlayback.Metadata.Interfaces;
using CtfPlayback.Metadata.InternalHelpers;
using CtfPlayback.Metadata.NamedScopes;
using CtfPlayback.Metadata.TypeInterfaces;
using CtfPlayback.Metadata.Types;

namespace CtfPlayback.Metadata.Ctf2
{
    /// <summary>
    /// Parses CTF 2 metadata (a JSON text sequence of fragments, see https://diamon.org/ctf/) into the same
    /// descriptor model used for CTF 1.8, so that the rest of the playback pipeline is format-agnostic.
    /// </summary>
    public sealed class Ctf2MetadataParser
        : ICtfMetadataParser
    {
        /// <summary>
        /// Each CTF 2 metadata fragment is preceded by this record separator (RFC 7464).
        /// </summary>
        public const char RecordSeparator = '\u001e';

        private const string DefaultClockTimestampRole = "default-clock-timestamp";
        private const string PacketEndDefaultClockTimestampRole = "packet-end-default-clock-timestamp";

        private readonly ICtfMetadataBuilder metadataBuilder;
        private readonly bool prefixEventFieldNamesWithUnderscore;

        private readonly Dictionary<string, object> aliases = new Dictionary<string, object>(StringComparer.Ordinal);

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="metadataBuilder">The object used to build metadata</param>
        /// <param name="prefixEventFieldNamesWithUnderscore">
        /// CTF 1.8 LTTng traces prefix event context and payload field names with '_'. Set this to produce
        /// the same field names from CTF 2 metadata.
        /// </param>
        public Ctf2MetadataParser(ICtfMetadataBuilder metadataBuilder, bool prefixEventFieldNamesWithUnderscore)
        {
            this.metadataBuilder = metadataBuilder ?? throw new ArgumentNullException(nameof(metadataBuilder));
            this.prefixEventFieldNamesWithUnderscore = prefixEventFieldNamesWithUnderscore;
        }

        /// <summary>
        /// Determines whether the metadata text is CTF 2 metadata.
        /// </summary>
        /// <param name="metadataText">Metadata text, with any packetization removed</param>
        /// <returns>true if the text is a CTF 2 JSON text sequence</returns>
        public static bool IsCtf2Metadata(string metadataText)
        {
            if (metadataText == null)
            {
                return false;
            }

            foreach (char c in metadataText)
            {
                if (c == RecordSeparator)
                {
                    return true;
                }

                if (!char.IsWhiteSpace(c))
                {
                    return false;
                }
            }

            return false;
        }

        /// <inheritdoc />
        public ICtfMetadata Parse(Stream metadataStream)
        {
            if (metadataStream == null)
            {
                throw new ArgumentNullException(nameof(metadataStream));
            }

            string text = CtfMetadataText.Read(CtfMetadataText.ReadAllBytes(metadataStream));
            return this.Parse(text);
        }

        /// <summary>
        /// Parses CTF 2 metadata text.
        /// </summary>
        /// <param name="metadataText">Metadata text, with any packetization removed</param>
        /// <returns>The parsed metadata</returns>
        public ICtfMetadata Parse(string metadataText)
        {
            var fragments = metadataText
                .Split(RecordSeparator)
                .Where(fragment => !string.IsNullOrWhiteSpace(fragment))
                .Select(fragment => Ctf2Json.Parse(fragment) as Dictionary<string, object>
                                    ?? throw new CtfMetadataException("CTF 2 metadata fragment is not a JSON object."))
                .ToList();

            if (fragments.Count == 0 || GetString(fragments[0], "type") != "preamble")
            {
                throw new CtfMetadataException("CTF 2 metadata does not start with a preamble fragment.");
            }

            var preamble = fragments[0];
            if (!TryGetNumber(preamble, "version", out var version) || version.AsLong() != 2)
            {
                throw new CtfMetadataException($"Unsupported CTF metadata version: {GetValue(preamble, "version")}.");
            }

            Dictionary<string, object> traceClass = null;
            var clockClasses = new List<Dictionary<string, object>>();
            var dataStreamClasses = new List<Dictionary<string, object>>();
            var eventRecordClasses = new List<Dictionary<string, object>>();

            foreach (var fragment in fragments.Skip(1))
            {
                string type = GetString(fragment, "type");
                switch (type)
                {
                    case "field-class-alias":
                        this.aliases[GetRequiredString(fragment, "name")] = GetRequiredValue(fragment, "field-class");
                        break;
                    case "trace-class":
                        traceClass = fragment;
                        break;
                    case "clock-class":
                        clockClasses.Add(fragment);
                        break;
                    case "data-stream-class":
                        dataStreamClasses.Add(fragment);
                        break;
                    case "event-record-class":
                        eventRecordClasses.Add(fragment);
                        break;
                    case "preamble":
                        throw new CtfMetadataException("CTF 2 metadata contains more than one preamble fragment.");
                    default:
                        // Unknown fragment types are ignored so newer metadata can still be read.
                        break;
                }
            }

            if (traceClass == null)
            {
                throw new CtfMetadataException("CTF 2 metadata does not contain a trace-class fragment.");
            }

            this.AddTraceClass(traceClass, preamble);

            foreach (var clockClass in clockClasses)
            {
                this.metadataBuilder.AddClock(CreateClock(clockClass));
            }

            // Streams are looked up by id, and CTF 1.8 streams are declared in id order. Keep that ordering.
            foreach (var dataStreamClass in dataStreamClasses.OrderBy(GetDataStreamClassId))
            {
                this.AddDataStreamClass(dataStreamClass);
            }

            foreach (var eventRecordClass in eventRecordClasses)
            {
                this.AddEventRecordClass(eventRecordClass);
            }

            return this.metadataBuilder;
        }

        private void AddTraceClass(Dictionary<string, object> traceClass, Dictionary<string, object> preamble)
        {
            var packetHeaderClass = GetValue(traceClass, "packet-header-field-class");
            if (packetHeaderClass == null)
            {
                throw new CtfMetadataException("CTF 2 trace-class does not define a packet header field class.");
            }

            if (!(this.BuildFieldClass(packetHeaderClass, BuildContext.Header(null)) is CtfStructDescriptor packetHeader))
            {
                throw new CtfMetadataException("CTF 2 trace-class packet header field class is not a structure.");
            }

            var traceBag = new CtfPropertyBag();
            traceBag.AddValue("major", "2");
            traceBag.AddValue("minor", "0");
            traceBag.AddValue("byte_order", "le");
            string uid = GetString(traceClass, "uid") ?? FormatUuid(GetValue(preamble, "uuid"));
            if (uid != null && Guid.TryParse(uid, out _))
            {
                traceBag.AddValue("uuid", uid);
            }

            this.metadataBuilder.SetTraceDescriptor(new CtfTraceDescriptor(traceBag, packetHeader));

            var environmentBag = new CtfPropertyBag();
            if (GetValue(traceClass, "environment") is Dictionary<string, object> environment)
            {
                foreach (var entry in environment)
                {
                    environmentBag.AddValue(entry.Key, Convert.ToString(entry.Value, CultureInfo.InvariantCulture));
                }
            }

            this.metadataBuilder.SetEnvironmentDescriptor(new CtfEnvironmentDescriptor(environmentBag));
        }

        private static CtfClockDescriptor CreateClock(Dictionary<string, object> clockClass)
        {
            // CTF 2 data stream classes refer to clock classes by id; older drafts only had a name.
            string clockName = GetString(clockClass, "id") ?? GetRequiredString(clockClass, "name");

            ulong frequency = TryGetNumber(clockClass, "frequency", out var frequencyNumber) ? frequencyNumber.AsULong() : 1000000000ul;

            var bag = new CtfPropertyBag();
            bag.AddValue("name", clockName);
            bag.AddValue("freq", frequency.ToString(CultureInfo.InvariantCulture));

            string description = GetString(clockClass, "description");
            if (description != null)
            {
                bag.AddValue("description", description);
            }

            string uid = GetString(clockClass, "uid") ?? FormatUuid(GetValue(clockClass, "uuid"));
            if (uid != null && Guid.TryParse(uid, out _))
            {
                bag.AddValue("uuid", uid);
            }

            if (TryGetNumber(clockClass, "precision", out var precision) && !precision.IsNegative)
            {
                bag.AddValue("precision", precision.Text);
            }

            if (GetValue(clockClass, "offset-from-origin") is Dictionary<string, object> offset)
            {
                // The descriptor expects the full offset in clock cycles.
                decimal cycles = 0;
                if (TryGetNumber(offset, "seconds", out var seconds))
                {
                    cycles += decimal.Parse(seconds.Text, CultureInfo.InvariantCulture) * frequency;
                }

                if (TryGetNumber(offset, "cycles", out var offsetCycles))
                {
                    cycles += decimal.Parse(offsetCycles.Text, CultureInfo.InvariantCulture);
                }

                if (cycles > 0)
                {
                    bag.AddValue("offset", decimal.ToUInt64(cycles).ToString(CultureInfo.InvariantCulture));
                }
            }

            bag.AddValue("absolute", GetValue(clockClass, "origin") != null ? "true" : "false");

            return new CtfClockDescriptor(bag);
        }

        private void AddDataStreamClass(Dictionary<string, object> dataStreamClass)
        {
            uint id = GetDataStreamClassId(dataStreamClass);
            string clockName = GetString(dataStreamClass, "default-clock-class-id");

            var packetContextClass = GetValue(dataStreamClass, "packet-context-field-class")
                ?? throw new CtfMetadataException($"CTF 2 data stream class {id} does not define a packet context.");
            var eventHeaderClass = GetValue(dataStreamClass, "event-record-header-field-class")
                ?? throw new CtfMetadataException($"CTF 2 data stream class {id} does not define an event record header.");

            var headerContext = BuildContext.Header(clockName);

            if (!(this.BuildFieldClass(packetContextClass, headerContext) is CtfStructDescriptor packetContext))
            {
                throw new CtfMetadataException($"CTF 2 data stream class {id} packet context is not a structure.");
            }

            if (!(this.BuildFieldClass(eventHeaderClass, headerContext) is CtfStructDescriptor eventHeader))
            {
                throw new CtfMetadataException($"CTF 2 data stream class {id} event record header is not a structure.");
            }

            CtfStructDescriptor eventContext = null;
            var eventContextClass = GetValue(dataStreamClass, "event-record-common-context-field-class");
            if (eventContextClass != null)
            {
                eventContext = this.BuildFieldClass(eventContextClass, this.EventContext(clockName)) as CtfStructDescriptor
                    ?? throw new CtfMetadataException($"CTF 2 data stream class {id} event record common context is not a structure.");
            }

            var bag = new CtfPropertyBag();
            bag.AddValue("id", id.ToString(CultureInfo.InvariantCulture));

            this.metadataBuilder.AddStream(new CtfStreamDescriptor(bag, eventHeader, eventContext, packetContext));
        }

        private void AddEventRecordClass(Dictionary<string, object> eventRecordClass)
        {
            string name = GetString(eventRecordClass, "name") ?? string.Empty;
            try
            {
                uint streamId = TryGetNumber(eventRecordClass, "data-stream-class-id", out var streamIdNumber) ? (uint)streamIdNumber.AsULong() : 0;
                ulong id = TryGetNumber(eventRecordClass, "id", out var idNumber) ? idNumber.AsULong() : 0;

                var assignments = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["name"] = name,
                    ["id"] = id.ToString(CultureInfo.InvariantCulture),
                    ["stream_id"] = streamId.ToString(CultureInfo.InvariantCulture),
                };

                if (TryGetLogLevel(eventRecordClass, out string logLevel))
                {
                    assignments["loglevel"] = logLevel;
                }

                var context = this.EventContext(null);
                var typeDeclarations = new Dictionary<string, ICtfTypeDescriptor>(StringComparer.Ordinal);

                var payloadClass = GetValue(eventRecordClass, "payload-field-class");
                typeDeclarations["fields"] = payloadClass != null
                    ? this.BuildFieldClass(payloadClass, context)
                    : new CtfStructDescriptor(new CtfPropertyBag(), Array.Empty<ICtfFieldDescriptor>());

                var specificContextClass = GetValue(eventRecordClass, "specific-context-field-class");
                if (specificContextClass != null)
                {
                    typeDeclarations["context"] = this.BuildFieldClass(specificContextClass, context);
                }

                this.metadataBuilder.AddEvent(assignments, typeDeclarations);
            }
            catch (Exception e) when (!(e is CtfMetadataException))
            {
                throw new CtfMetadataException($"Unable to process CTF 2 event record class '{name}': {e.Message}", e);
            }
        }

        private BuildContext EventContext(string clockName)
        {
            return new BuildContext(clockName, this.prefixEventFieldNamesWithUnderscore);
        }

        private CtfMetadataTypeDescriptor BuildFieldClass(object fieldClass, BuildContext context, IReadOnlyList<SelectorOption> selectorOptions = null)
        {
            var fieldClassObject = this.ResolveFieldClass(fieldClass);
            string type = GetRequiredString(fieldClassObject, "type");

            switch (type)
            {
                case "fixed-length-unsigned-integer":
                case "fixed-length-bit-array":
                case "fixed-length-bit-map":
                case "fixed-length-boolean":
                    return BuildFixedLengthInteger(fieldClassObject, false, context, selectorOptions);

                case "fixed-length-signed-integer":
                    return BuildFixedLengthInteger(fieldClassObject, true, context, selectorOptions);

                case "variable-length-unsigned-integer":
                case "variable-length-signed-integer":
                    if (selectorOptions != null)
                    {
                        throw new CtfMetadataException("Variable-length integer variant selectors are not supported.");
                    }

                    return new Ctf2VariableLengthIntegerDescriptor(
                        type == "variable-length-signed-integer",
                        GetDisplayBase(fieldClassObject),
                        GetClockMap(fieldClassObject, context));

                case "fixed-length-floating-point-number":
                case "fixed-length-floating-point":
                    return BuildFloatingPoint(fieldClassObject);

                case "null-terminated-string":
                    EnsureUtf8(fieldClassObject);
                    var stringBag = new CtfPropertyBag();
                    stringBag.AddValue("encoding", "UTF8");
                    return new CtfStringDescriptor(stringBag);

                case "static-length-string":
                    EnsureUtf8(fieldClassObject);
                    return new CtfArrayDescriptor(CreateByte("UTF8", 10), GetRequiredNumber(fieldClassObject, "length").AsULong().ToString(CultureInfo.InvariantCulture));

                case "dynamic-length-string":
                    EnsureUtf8(fieldClassObject);
                    return new CtfArrayDescriptor(CreateByte("UTF8", 10), this.GetFieldLocation(fieldClassObject, "length-field-location", context));

                case "static-length-blob":
                    return new CtfArrayDescriptor(CreateByte(null, 16), GetRequiredNumber(fieldClassObject, "length").AsULong().ToString(CultureInfo.InvariantCulture));

                case "dynamic-length-blob":
                    return new CtfArrayDescriptor(CreateByte(null, 16), this.GetFieldLocation(fieldClassObject, "length-field-location", context));

                case "static-length-array":
                    return new CtfArrayDescriptor(
                        this.BuildArrayElement(fieldClassObject, context),
                        GetRequiredNumber(fieldClassObject, "length").AsULong().ToString(CultureInfo.InvariantCulture));

                case "dynamic-length-array":
                    return new CtfArrayDescriptor(
                        this.BuildArrayElement(fieldClassObject, context),
                        this.GetFieldLocation(fieldClassObject, "length-field-location", context));

                case "structure":
                    return this.BuildStructure(fieldClassObject, context);

                case "variant":
                    return this.BuildVariant(fieldClassObject, context);

                default:
                    throw new CtfMetadataException($"CTF 2 field class type '{type}' is not supported.");
            }
        }

        private Dictionary<string, object> ResolveFieldClass(object fieldClass)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            while (fieldClass is string aliasName)
            {
                if (!seen.Add(aliasName) || !this.aliases.TryGetValue(aliasName, out fieldClass))
                {
                    throw new CtfMetadataException($"Unknown CTF 2 field class alias '{aliasName}'.");
                }
            }

            return fieldClass as Dictionary<string, object>
                ?? throw new CtfMetadataException("CTF 2 field class is not a JSON object.");
        }

        private static CtfMetadataTypeDescriptor BuildFixedLengthInteger(
            Dictionary<string, object> fieldClass,
            bool signed,
            BuildContext context,
            IReadOnlyList<SelectorOption> selectorOptions)
        {
            // Like the CTF 1.8 path, only whole-byte big-endian integers are byte swapped when read; sub-byte
            // big-endian bit fields (e.g. IPv4 header fields in lttng-modules net_* events) are still readable.
            bool bigEndian = GetString(fieldClass, "byte-order") == "big-endian";
            var length = GetRequiredNumber(fieldClass, "length");

            var bag = new CtfPropertyBag();
            bag.AddValue("size", length.Text);
            bag.AddValue("align", GetAlignment(fieldClass, "alignment").ToString(CultureInfo.InvariantCulture));
            bag.AddValue("signed", signed ? "true" : "false");
            bag.AddValue("byte_order", bigEndian ? "be" : "le");
            bag.AddValue("base", GetDisplayBase(fieldClass).ToString(CultureInfo.InvariantCulture));

            string map = GetClockMap(fieldClass, context);
            if (map != null)
            {
                bag.AddValue("map", map);
            }

            var integer = new CtfIntegerDescriptor(bag);
            if (selectorOptions == null)
            {
                return integer;
            }

            // Variant selection in this library is done through an enumeration whose labels are the option names.
            var enumeration = new CtfEnumDescriptor(integer);
            foreach (var option in selectorOptions)
            {
                foreach (var range in option.Ranges)
                {
                    var begin = CreateIntegerLiteral(range.Item1, signed);
                    var end = CreateIntegerLiteral(range.Item2, signed);
                    if (!enumeration.AddRange(option.Name, new CtfIntegerRange(integer, begin, end)))
                    {
                        throw new CtfMetadataException($"Unable to add variant selector range for option '{option.Name}'.");
                    }
                }
            }

            return enumeration;
        }

        private static CtfMetadataTypeDescriptor BuildFloatingPoint(Dictionary<string, object> fieldClass)
        {
            var bag = new CtfPropertyBag();
            switch (GetRequiredNumber(fieldClass, "length").AsLong())
            {
                case 32:
                    bag.AddValue("exp_dig", "8");
                    bag.AddValue("mant_dig", "24");
                    break;
                case 64:
                    bag.AddValue("exp_dig", "11");
                    bag.AddValue("mant_dig", "53");
                    break;
                default:
                    throw new CtfMetadataException("Only 32-bit and 64-bit CTF 2 floating point numbers are supported.");
            }

            bag.AddValue("align", GetAlignment(fieldClass, "alignment").ToString(CultureInfo.InvariantCulture));
            bag.AddValue("byte_order", GetString(fieldClass, "byte-order") == "big-endian" ? "be" : "le");
            return new CtfFloatingPointDescriptor(bag);
        }

        private CtfMetadataTypeDescriptor BuildArrayElement(Dictionary<string, object> fieldClass, BuildContext context)
        {
            var element = this.BuildFieldClass(GetRequiredValue(fieldClass, "element-field-class"), context);
            if (TryGetNumber(fieldClass, "minimum-alignment", out var minimumAlignment) && minimumAlignment.AsLong() > element.Align)
            {
                throw new CtfMetadataException("CTF 2 arrays with a minimum alignment greater than their element alignment are not supported.");
            }

            return element;
        }

        private CtfStructDescriptor BuildStructure(Dictionary<string, object> fieldClass, BuildContext context)
        {
            var members = GetValue(fieldClass, "member-classes") as List<object> ?? new List<object>();
            var memberObjects = members.Select(member => member as Dictionary<string, object>
                ?? throw new CtfMetadataException("CTF 2 structure member class is not a JSON object.")).ToList();

            // A variant selects its option through a sibling integer field; collect the option ranges so that
            // the selector can be built as an enumeration.
            var selectorOptionsByMember = new Dictionary<string, List<SelectorOption>>(StringComparer.Ordinal);
            foreach (var member in memberObjects)
            {
                var memberClass = this.ResolveFieldClass(GetRequiredValue(member, "field-class"));
                if (GetString(memberClass, "type") != "variant")
                {
                    continue;
                }

                string selectorName = GetSiblingSelectorName(memberClass);
                if (!selectorOptionsByMember.TryGetValue(selectorName, out var selectorOptions))
                {
                    selectorOptions = new List<SelectorOption>();
                    selectorOptionsByMember.Add(selectorName, selectorOptions);
                }

                selectorOptions.AddRange(GetVariantOptions(memberClass, context));
            }

            var fields = new List<ICtfFieldDescriptor>(memberObjects.Count);
            foreach (var member in memberObjects)
            {
                string memberName = GetRequiredString(member, "name");
                selectorOptionsByMember.TryGetValue(memberName, out var selectorOptions);
                var memberType = this.BuildFieldClass(GetRequiredValue(member, "field-class"), context, selectorOptions);
                fields.Add(new CtfFieldDescriptor(memberType, context.MapName(memberName)));
            }

            var bag = new CtfPropertyBag();
            bag.AddValue("align", GetAlignment(fieldClass, "minimum-alignment").ToString(CultureInfo.InvariantCulture));
            return new CtfStructDescriptor(bag, fields.ToArray());
        }

        private CtfVariantDescriptor BuildVariant(Dictionary<string, object> fieldClass, BuildContext context)
        {
            string selectorName = context.MapName(GetSiblingSelectorName(fieldClass));

            var options = GetValue(fieldClass, "options") as List<object>
                ?? throw new CtfMetadataException("CTF 2 variant does not define any options.");

            var fields = new List<ICtfFieldDescriptor>(options.Count);
            for (int index = 0; index < options.Count; index++)
            {
                var option = options[index] as Dictionary<string, object>
                    ?? throw new CtfMetadataException("CTF 2 variant option is not a JSON object.");
                var optionType = this.BuildFieldClass(GetRequiredValue(option, "field-class"), context);
                fields.Add(new CtfFieldDescriptor(optionType, GetOptionName(option, index, context)));
            }

            return new CtfVariantDescriptor(selectorName, fields);
        }

        private static IEnumerable<SelectorOption> GetVariantOptions(Dictionary<string, object> variantClass, BuildContext context)
        {
            var options = GetValue(variantClass, "options") as List<object>
                ?? throw new CtfMetadataException("CTF 2 variant does not define any options.");

            for (int index = 0; index < options.Count; index++)
            {
                var option = (Dictionary<string, object>)options[index];
                var ranges = GetValue(option, "selector-field-ranges") as List<object>
                    ?? throw new CtfMetadataException("CTF 2 variant option does not define selector field ranges.");

                var optionRanges = new List<Tuple<Ctf2JsonNumber, Ctf2JsonNumber>>();
                foreach (var range in ranges)
                {
                    if (!(range is List<object> bounds) || bounds.Count != 2 ||
                        !(bounds[0] is Ctf2JsonNumber lower) || !(bounds[1] is Ctf2JsonNumber upper))
                    {
                        throw new CtfMetadataException("CTF 2 variant selector field range is invalid.");
                    }

                    optionRanges.Add(Tuple.Create(lower, upper));
                }

                yield return new SelectorOption(GetOptionName(option, index, context), optionRanges);
            }
        }

        private static string GetOptionName(Dictionary<string, object> option, int index, BuildContext context)
        {
            string name = GetString(option, "name");
            return name == null ? $"option{index}" : context.MapName(name);
        }

        private static string GetSiblingSelectorName(Dictionary<string, object> variantClass)
        {
            var path = GetFieldLocationPath(variantClass, "selector-field-location");
            if (path.Count != 1)
            {
                throw new CtfMetadataException("Only CTF 2 variants whose selector is a sibling field are supported.");
            }

            return path[0];
        }

        private string GetFieldLocation(Dictionary<string, object> fieldClass, string property, BuildContext context)
        {
            return string.Join(".", GetFieldLocationPath(fieldClass, property).Select(context.MapName));
        }

        private static List<string> GetFieldLocationPath(Dictionary<string, object> fieldClass, string property)
        {
            if (!(GetValue(fieldClass, property) is Dictionary<string, object> location) ||
                !(GetValue(location, "path") is List<object> path) ||
                path.Count == 0)
            {
                throw new CtfMetadataException($"CTF 2 field class has an invalid '{property}'.");
            }

            return path.Select(element => element as string
                ?? throw new CtfMetadataException($"CTF 2 '{property}' path elements must be field names.")).ToList();
        }

        private static CtfIntegerDescriptor CreateByte(string encoding, int displayBase)
        {
            var bag = new CtfPropertyBag();
            bag.AddValue("size", "8");
            bag.AddValue("align", "8");
            bag.AddValue("signed", "false");
            bag.AddValue("byte_order", "le");
            bag.AddValue("base", displayBase.ToString(CultureInfo.InvariantCulture));
            if (encoding != null)
            {
                bag.AddValue("encoding", encoding);
            }

            return new CtfIntegerDescriptor(bag);
        }

        private static string GetClockMap(Dictionary<string, object> fieldClass, BuildContext context)
        {
            if (context.ClockName == null || !(GetValue(fieldClass, "roles") is List<object> roles))
            {
                return null;
            }

            bool isTimestamp = roles.OfType<string>().Any(role =>
                role == DefaultClockTimestampRole || role == PacketEndDefaultClockTimestampRole);

            return isTimestamp ? $"clock.{context.ClockName}.value" : null;
        }

        private static int GetDisplayBase(Dictionary<string, object> fieldClass)
        {
            return TryGetNumber(fieldClass, "preferred-display-base", out var displayBase) ? (int)displayBase.AsLong() : 10;
        }

        private static long GetAlignment(Dictionary<string, object> fieldClass, string property)
        {
            return TryGetNumber(fieldClass, property, out var alignment) ? alignment.AsLong() : 1;
        }

        private static void EnsureUtf8(Dictionary<string, object> fieldClass)
        {
            string encoding = GetString(fieldClass, "encoding");
            if (encoding != null && encoding != "utf-8")
            {
                throw new CtfMetadataException($"CTF 2 string encoding '{encoding}' is not supported.");
            }
        }

        private static IntegerLiteral CreateIntegerLiteral(Ctf2JsonNumber number, bool signed)
        {
            return signed ? new IntegerLiteral(number.AsLong()) : new IntegerLiteral(number.AsULong());
        }

        private static bool TryGetLogLevel(Dictionary<string, object> eventRecordClass, out string logLevel)
        {
            logLevel = null;
            if (!(GetValue(eventRecordClass, "attributes") is Dictionary<string, object> attributes))
            {
                return false;
            }

            foreach (var namespacedAttributes in attributes.Values.OfType<Dictionary<string, object>>())
            {
                if (TryGetNumber(namespacedAttributes, "log-level", out var level) && !level.IsNegative)
                {
                    logLevel = level.Text;
                    return true;
                }
            }

            return false;
        }

        private static uint GetDataStreamClassId(Dictionary<string, object> dataStreamClass)
        {
            return TryGetNumber(dataStreamClass, "id", out var id) ? (uint)id.AsULong() : 0;
        }

        private static string FormatUuid(object uuid)
        {
            if (!(uuid is List<object> bytes) || bytes.Count != 16 || !bytes.All(b => b is Ctf2JsonNumber))
            {
                return null;
            }

            var hex = string.Concat(bytes.Cast<Ctf2JsonNumber>().Select(b => b.AsULong().ToString("x2", CultureInfo.InvariantCulture)));
            return $"{hex.Substring(0, 8)}-{hex.Substring(8, 4)}-{hex.Substring(12, 4)}-{hex.Substring(16, 4)}-{hex.Substring(20)}";
        }

        private static object GetValue(Dictionary<string, object> json, string property)
        {
            return json.TryGetValue(property, out var value) ? value : null;
        }

        private static object GetRequiredValue(Dictionary<string, object> json, string property)
        {
            return GetValue(json, property) ?? throw new CtfMetadataException($"CTF 2 metadata is missing the required '{property}' property.");
        }

        private static string GetString(Dictionary<string, object> json, string property)
        {
            return GetValue(json, property) as string;
        }

        private static string GetRequiredString(Dictionary<string, object> json, string property)
        {
            return GetString(json, property) ?? throw new CtfMetadataException($"CTF 2 metadata is missing the required '{property}' string property.");
        }

        private static bool TryGetNumber(Dictionary<string, object> json, string property, out Ctf2JsonNumber number)
        {
            number = GetValue(json, property) as Ctf2JsonNumber;
            return number != null;
        }

        private static Ctf2JsonNumber GetRequiredNumber(Dictionary<string, object> json, string property)
        {
            return TryGetNumber(json, property, out var number)
                ? number
                : throw new CtfMetadataException($"CTF 2 metadata is missing the required '{property}' number property.");
        }

        private sealed class BuildContext
        {
            internal BuildContext(string clockName, bool prefixNames)
            {
                this.ClockName = clockName;
                this.PrefixNames = prefixNames;
            }

            internal string ClockName { get; }

            internal bool PrefixNames { get; }

            internal static BuildContext Header(string clockName) => new BuildContext(clockName, false);

            internal string MapName(string name) => this.PrefixNames ? "_" + name : name;
        }

        private sealed class SelectorOption
        {
            internal SelectorOption(string name, IReadOnlyList<Tuple<Ctf2JsonNumber, Ctf2JsonNumber>> ranges)
            {
                this.Name = name;
                this.Ranges = ranges;
            }

            internal string Name { get; }

            internal IReadOnlyList<Tuple<Ctf2JsonNumber, Ctf2JsonNumber>> Ranges { get; }
        }
    }
}
