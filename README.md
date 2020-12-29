# Microsoft Performance Tools Linux

> This repo contains various Linux Performance Analysis tools built with the [Microsoft Performance Toolkit SDK](https://github.com/microsoft/microsoft-performance-toolkit-sdk).

> Tools are built with open source .NET Core and can be run on the cmd-line or in the WPA GUI. All the logs that are supported are open source. 

>Not only are the raw logs parsed, but a lot of smart post processing / correlation is done to make your life easier as a perf analyst. We hope you can solve & debug tough issues on you or your customers systems with this toolset!

> Tracing supported: [LTTng](https://lttng.org) (Kernel CPU scheduling, Processes, Threads, Block IO/Disk, Syscalls, File events, etc), [perf](https://perf.wiki.kernel.org/) CPU Sampling(cpu-clock)

> Logs supported: [Dmesg](https://en.wikipedia.org/wiki/Dmesg), [Cloud-Init](https://cloud-init.io/), [WaLinuxAgent](https://github.com/Azure/WALinuxAgent)

**Optional** WPA GUI:
![WpaLinux](Images/WpaLinux.JPG)

# Presentations

If you want to see a demo or get more in-depth info on using these tools check out a talk given at the [Linux Tracing Summit](https://www.tracingsummit.org/ts/2019/):
>Linux & Windows Perf Analysis using WPA, ([slides](https://www.tracingsummit.org/ts/2019/files/Tracingsummit2019-wpa-berg-gibeau.pdf)) ([video](https://youtu.be/HUbVaIi-aaw))

# Prerequisites

## Runtime prereqs
- [.NET Core Runtime 3.1.x](https://dotnet.microsoft.com/download/dotnet-core/3.1)

## Dev prereqs
- [.NET Core SDK 3.1.x](https://dotnet.microsoft.com/download/dotnet-core/3.1)
- [Visual Studio](https://visualstudio.microsoft.com/), [VSCode](https://visualstudio.microsoft.com/), or your favorite editor!

# Download
See [Releases](https://github.com/microsoft/Microsoft-Performance-Tools-Linux/releases)

# How to run the tools
The tools can be run in several modes:

- Cross-platform with .NET Core
  - Used as a library to process traces / logs programatically
    - Examples: 
    - [LTTng 1](LTTngDriver/Program.cs), [LTTng 2](LTTngDataExtUnitTest/LTTngUnitTest.cs)
    - [Perf](PerfUnitTest/PerfUnitTest.cs)
    - [LinuxLogs](LinuxLogParsers/LinuxLogParsersUnitTest/LinuxLogParsersUnitTest.cs)
  - With a driver program for example dumping to screen or text format
    - ./LTTngDriver.exe LTTng-Kernel-Trace.ctf (trace folder is zipped and renamed to .ctf)
    - ./LTTngDriver.exe LTTngKernelTraceFolder (not currently working - blocked on Issue #6)
- (Coming soon) (Windows) Command-line dumping to a text format (say CSV)
- (Coming soon) (Windows) Using the WPA GUI to load these tools as plugins

# How to capture a trace or logs
Please see [Linux Trace Log Capture](LinuxTraceLogCapture.md)

# How to load the logs in the UI
Once you gather the data, there is a tiny bit of prep needed to open them in a single unified timeline (like the screenshot above)

- LTTng - If you just need to open only a LTTng trace by itself in folder format
  - WPA -> Open -> Folder (Select CTF folder)
- Unified (LTTng or other multiple different logs files together)
  - If you want to open other logs together in single timeline - Copy other Linux logs you want to open to single folder
  - Example: You want to open in the same timeline: LTTng, Perf CPU Sampling, Dmesg
  - Ensure that the Linux CTF folder/trace is zipped and renamed to .ctf in the same folder (hack so open Unified works)
  - WPA -> File -> Open -> Multi-select all files and choose "Open Unified"

# How do I use WPA in general?
If you want to learn how to use the GUI UI in general see [WPA MSDN Docs](https://docs.microsoft.com/en-us/windows-hardware/test/wpt/windows-performance-analyzer)

## Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

## Trademarks

This project may contain trademarks or logos for projects, products, or services. Authorized use of Microsoft 
trademarks or logos is subject to and must follow 
[Microsoft's Trademark & Brand Guidelines](https://www.microsoft.com/en-us/legal/intellectualproperty/trademarks/usage/general).
Use of Microsoft trademarks or logos in modified versions of this project must not cause confusion or imply Microsoft sponsorship.
Any use of third-party trademarks or logos are subject to those third-party's policies.
