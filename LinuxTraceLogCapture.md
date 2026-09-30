# Overview - Linux Trace and Log Capture

This provides a quick start on how to capture logs on Linux. 

Logs:

- [LTTng](https://lttng.org) system trace (requires customized image for boot scenario)
- [perf](https://perf.wiki.kernel.org/)
- [cloud-init.log](https://cloud-init.io/)
  - Automatically logged by cloud-init to /var/log/cloud-init.log
- [dmesg.iso.log](https://en.wikipedia.org/wiki/Dmesg)
  - Standard auto dmesg log doesn't output in absolute time needed for correlation with other logs
  - dmesg --time-format iso > dmesg.iso.log
- [waagent.log](https://github.com/Azure/WALinuxAgent)
  - Automatically logged by WaLinuxAgent to /var/log/waagent.log
  - LogLevel can be turned more verbose in custom image
  - /etc/waagent.conf
  - Logs.Verbose=y
- [AndroidLogcat](https://developer.android.com/studio/command-line/logcat)
  - Default log format should be supported
  - Basic durations are supported/parsed on production builds / logs.
    - E.g. "SurfaceFlinger: Boot is finished (9550 ms)"
  - Enhanced durations are supported/parsed, if the logging includes init timing or *Timing logs such as SystemServerTiming. Perf durations are available in-development with [userdebug builds](https://source.android.com/setup/develop/new-device#userdebug-guidelines). 
      - E.g "SystemServerTimingAsync: InitThreadPoolExec:prepareAppData took to complete: 149ms"
  - Logcat can optionally log the year but defaults to not. If the year is not provided the year is assumed to be the current year on the analysis machine.
    - If this is incorrect, for example trace was captured in 2021, but analyzed in 2022 then the year will be interpreted incorrectly. 
    - This applies only if you need correct absolute timestamps, as relative timestamps will still be good.
    - Manual workaround: In the logcat log search/replace to add year. E.g. "12-21" -> "12-21-2021"
  - Logs are logged in local time zone and default assumed to be loaded in same time zone as captured
  - To provide a hint on the timezone if in a different zone
    - Place a "utcoffset.txt" file in the same folder as the trace. Place the UTC+Offset in the file as a double in hours. 
    - E.g For India Standard Time (IST) offset is UTC+5.5 so place "5.5" in the file. If logs are in UTC place 0 in the file

# LTTng
[LTTng](https://lttng.org) (Kernel CPU scheduling, Processes, Threads, Block IO/Disk, Syscalls, File events, etc)

[LTTng Docs](https://lttng.org/docs/v2.10/) [LTTng](https://lttng.org/) is an open source tracing framework for Linux. Installation instructions for your Linux distro can be found in the docs. 

Supports:
- Threads and Processes
- Context Switches / CPU Usage
- Syscalls
- File related events
- Block IO / Disk Activity
- Diagnostic Messages

Traces in both the CTF 1.8 and CTF 2 formats are supported (CTF 2 is the default trace format since LTTng 2.15).

Once you have everything set up you just need to decide what kind of information you are looking for and begin tracing.

In this example we are looking at process scheduler events. We might use this to determine process lifetime and identify dependencies. You can learn more about what kind of "events" you can enable [here](https://lttng.org/man/1/lttng-enable-event/v2.8/). 
```bash
 root@xenial:~/tracing# lttng list --kernel # Gives a list of all Kernel events you can trace
 root@xenial:~/tracing# lttng list --kernel --syscall # Gives a list of all traceable Linux system calls

 root@xenial:~/tracing# lttng create my-kernel-session --output=/tmp/my-kernel-trace
 root@xenial:~/tracing# lttng enable-event --kernel sched_process*
 root@xenial:~/tracing# lttng start
 root@xenial:~/tracing# lttng stop
 root@xenial:~/tracing# lttng destroy
```

## Recommended LTTng Tracing 

### Install the tracing software:
Example on Ubuntu:
```bash
$ sudo apt-get install lttng-tools lttng-modules-dkms liblttng-ust-dev
```
For more examples see [LTTng Download docs](https://lttng.org/download/)

### Create a session:
```bash
$ sudo lttng create my-kernel-session --output=lttng-kernel-trace
```

### Add the desired events to be recorded:
```bash
$ sudo lttng enable-event --kernel block_rq_complete,block_rq_insert,block_rq_issue,printk_console,sched_wak*,sched_switch,sched_process_fork,sched_process_exit,sched_process_exec,lttng_statedump*
$ sudo lttng enable-event --kernel --syscall �-all
```

### Add context fields to the channel:
```bash
$ sudo lttng add-context --kernel --channel=channel0 --type=tid
$ sudo lttng add-context --kernel --channel=channel0 --type=pid
$ sudo lttng add-context --kernel --channel=channel0 --type=procname
```

### Optionally, add CPU performance counters:
The Execution Events table shows how much each counter changed while a thread was running (Instruction Count, CPU Cycle, LLC Misses, Cache Misses).
```bash
$ sudo lttng add-context --kernel --channel=channel0 --type=perf:cpu:instructions --type=perf:cpu:cycles --type=perf:cpu:cache-misses --type=perf:cpu:LLC-load-misses
```

### Start the recording:
```bash
$ sudo lttng start
```

### Save the session:
```bash
$ sudo lttng regenerate statedump <- Better correlation / info in Microsoft-Performance-Tools-Linux
$ sudo lttng stop
$ sudo lttng destroy
```

# Perf.data

You can collect and view Linux [kernel-mode](https://www.kernel.org/doc/html/latest/trace/tracepoints.html) and [user-mode](https://docs.kernel.org/trace/user_events.html) tracepoints in the `perf.data` file format.

- Select existing tracepoints that you want to collect, or write your own programs that generate tracepoint events.
- Use the Linux `perf` tool or some other tool to collect tracepoint events into `perf.data` files.
- Use the PerfData extension to view the tracepoints.

## Selecting tracepoints

The Linux kernel and the kernel modules generate many useful tracepoints that you can collect. Look in `/sys/kernel/tracing/events` for the tracepoints that are available to you.

In addition, Linux 6.4 adds support for generating
[user_events](https://docs.kernel.org/trace/user_events.html)
tracepoints from user-mode programs.

- [LinuxTracepoints](https://github.com/microsoft/LinuxTracepoints) contains support for generating `user_events` from C/C++ programs.
- [LinuxTracepoints-Rust](https://github.com/microsoft/LinuxTracepoints-Rust) contains support for generating `user_events` from Rust programs.

## Collecting tracepoints

The Linux [perf](https://perf.wiki.kernel.org/) tool supports collecting tracepoint events using `perf record`.

- Install `perf` from the `linux-perf` or `linux-tools` package.
  - Note that some `perf` packages use a wrapper script to help you match the running kernel version with a version-specific build of the `perf` tool, e.g. `perf_VERSION`. For collecting tracepoints, the version doesn't need to match. If you have version mismatch problems, you can safely bypass the wrapper script and directly use the `perf_VERSION` tool.
- Install the `libtraceevent1` package to enable `perf` support for tracepoints.
- Use [perf record](https://www.man7.org/linux/man-pages/man1/perf-record.1.html) to collect traces, e.g. `perf record -o MyFile.perf.data -k monotonic -e "event1_group:event1_name,event2_group:event2_name"`
  - Use `-k monotonic` to include clock offset information in the data file.

You can also use other tools that generate `perf.data`-compatible files.

- [libtracepoint-control](https://github.com/microsoft/LinuxTracepoints/tree/main/libtracepoint-control-cpp) includes a library for configuring tracepoint collection sessions and collecting `perf.data` files.
- [tracepoint-collect](https://github.com/microsoft/LinuxTracepoints/blob/main/libtracepoint-control-cpp/tools/tracepoint-collect.cpp) is a simple tool that collects tracepoint events into `perf.data` files.

# Perf.data.txt
Perf is used to collect CPU Sampling (cpu-clock) events as LTTng doesn't support capturing these yet. Note: Stacks may require symbol setup.

The perf CPU Sampling analysis plugin uses perf.data.txt files as input.

[perf](https://perf.wiki.kernel.org/) CPU Sampling(cpu-clock)

If you want to trace .NET Core then you need [perfcollect](http://aka.ms/perfcollect) which capture CPU sampling and more

## Perf Install
```bash
$ sudo apt-get install linux-tools-common
```

## User-Mode (UM) Symbols Install
KM symbols are automatically resolved. If you wish to resolve UM cpu sample functions and stacks, you may need to install debug packages for the binary you are profiling

For example, [Debug Symbol Packages on Ubuntu](https://wiki.ubuntu.com/Debug%20Symbol%20Packages)

## Record a trace
```bash
$ sudo /usr/bin/perf record -g -a -F 999 -e cpu-clock,sched:sched_stat_sleep,sched:sched_switch,sched:sched_process_exit -o perf_cpu.data
```

## Stop the Trace
```bash
$ Ctrl-C
```

## Convert trace to text format
This is to useful along-side the CTF trace to resolve UM IP/Symbols. Similar to what [perfcollect](https://raw.githubusercontent.com/microsoft/perfview/master/src/perfcollect/perfcollect) uses

```bash
$ sudo perf inject -v -s -i perf_cpu.data -o perf.data.merged

# There is a breaking change where the capitalization of the -f parameter changed.
$ sudo perf script -i perf.data.merged -F comm,pid,tid,cpu,time,period,event,ip,sym,dso,trace > perf.data.txt

if [ $? -ne 0 ]
then
    $ sudo perf script -i perf.data.merged -f comm,pid,tid,cpu,time,period,event,ip,sym,dso,trace > perf.data.txt
fi

# If the dump file is zero length, try to collect without the period field, which was added recently.
if [ ! -s perf.data.txt ]
then
    $ sudo perf script -i perf.data.merged -f comm,pid,tid,cpu,time,event,ip,sym,dso,trace > perf.data.txt
fi
```

## Capture trace timestamp start 
Perf.data.txt only contains relative timestamps. If you want correct absolute timestamps in UI then you will need to know the trace start time.

```bash
$ sudo perf report --header-only -i perf_cpu.data | grep "captured on"
```

Place the "captured on" timestamp for example "Thu Oct 17 15:37:36 2019" in a timestamp.txt file next to the trace folder. The timestamp will be interpreted as UTC

# Transferring the files to Windows UI (optional)
You then need to transfer the perf files to a Windows box where WPA runs. The most important file is perf.data.txt

```bash
$ sudo chmod 777 -R perf*
```

- Copy files from Linux to Windows box with WinSCP/SCP OR 
```bash
$ tar -czvf perf_cpu.tar.gz perf*
```
- (Optional if you want absolute timestamps) Place timestamp.txt next to perf.data.txt
- Open perf.data.txt with WPA


# Presentations

If you want to see a demo or get more in-depth info on using these tools check out a talk given at the [Linux Tracing Summit](https://www.tracingsummit.org/ts/2019/):
>Linux & Windows Perf Analysis using WPA, ([slides](https://www.tracingsummit.org/ts/2019/files/Tracingsummit2019-wpa-berg-gibeau.pdf)) ([video](https://youtu.be/HUbVaIi-aaw))
