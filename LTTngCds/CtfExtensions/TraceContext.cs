// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CtfPlayback.Metadata.Interfaces;

namespace LTTngCds.CtfExtensions
{
    internal class TraceContext
    {
        internal TraceContext(ICtfMetadata metadata)
        {
            if (metadata.EnvironmentDescriptor.Properties.TryGetValue("hostname", out string hostName))
            {
                this.HostName = hostName;
            }

            if (metadata.EnvironmentDescriptor.Properties.TryGetValue("domain", out string domain))
            {
                this.Domain = domain;
            }

            if (metadata.EnvironmentDescriptor.Properties.TryGetValue("sysname", out string sysName))
            {
                this.SysName = sysName;
            }

            if (metadata.EnvironmentDescriptor.Properties.TryGetValue("kernel_release", out string kernelRelease))
            {
                this.KernelRelease = kernelRelease;
            }

            if (metadata.EnvironmentDescriptor.Properties.TryGetValue("kernel_version", out string kernelVersion))
            {
                this.KernelVersion = kernelVersion;
            }

            if (metadata.EnvironmentDescriptor.Properties.TryGetValue("tracer_name", out string tracerName))
            {
                this.TracerName = tracerName;
            }

            if (metadata.EnvironmentDescriptor.Properties.TryGetValue("tracer_major", out string tracerMajor))
            {
                if (uint.TryParse(tracerMajor, out uint majorTracerVersion))
                {
                    this.TracerMajor = majorTracerVersion;
                }
            }

            if (metadata.EnvironmentDescriptor.Properties.TryGetValue("tracer_minor", out string tracerMinor))
            {
                if (uint.TryParse(tracerMinor, out uint minorTracerVersion))
                {
                    this.TracerMinor = minorTracerVersion;
                }
            }

            if (metadata.EnvironmentDescriptor.Properties.TryGetValue("tracer_patchlevel", out string tracerPatchLevel))
            {
                if (uint.TryParse(tracerPatchLevel, out uint patchLevelTracerVersion))
                {
                    this.TracerMajor = patchLevelTracerVersion;
                }
            }
        }

        public string HostName { get; }

        public string Domain { get; }

        public string SysName { get; }

        public string KernelRelease { get; }

        public string KernelVersion { get; }

        public string TracerName { get; }

        public uint TracerMajor { get; }

        public uint TracerMinor { get; }

        public uint TracerPathLevel { get; }

    }
}