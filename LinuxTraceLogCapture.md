# Overview - Linux Trace and Log Capture

This provides a quick start on how to capture logs on Linux. 

Logs:

- [LTTng](https://lttng.org) system trace (requires customized image for boot scenario)
- Perf CPU Sampling
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
$ sudo lttng enable-event --kernel --syscall –-all
```

### Add context fields to the channel:
```bash
$ sudo lttng add-context --kernel --channel=channel0 --type=tid
$ sudo lttng add-context --kernel --channel=channel0 --type=pid
$ sudo lttng add-context --kernel --channel=channel0 --type=procname
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

# Perf
Perf is used to collect CPU Sampling (cpu-clock) events as LTTng doesn't support capturing these yet. Note: Stacks may require symbol setup

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

### Convert to CTF (Optional) (requires CTF enabled perf) 
We have optional support for perf CTF conversion. It it currently NOT RECOMMENDED to use this though as you get less features (like KM/UM stacks) than perf.data.txt support which resolves callstacks on the box.
This only supports KM symbols (for now) supplied by kallsyms. Microsoft-Linux-Perf-Tools support for the perf CTF trace is experimental given lack of UM symbols

```bash
$ perf data convert -i perf_cpu.data --all --to-ctf ./perf_cpu.data-ctf
```

You will need the perf file in converted CTF format which you can do with a custom compiled perf (unless some distro compiled the support in). [Custom build instructions here](https://stackoverflow.com/questions/43576997/building-perf-with-babeltrace-for-perf-to-ctf-conversion)

## Save Kernel Symbols (Optional) (for use with CTF enabled perf)
This is only needed for a perf CTF trace

```bash
$ sudo cat /proc/kallsyms > kallsyms
```

# Transferring the files to Windows UI (optional)
You then need to transfer the perf files to a Windows box where WPA runs. The most important file is perf.data.txt

```bash
$ sudo chmod 777 -R perf*
```

- Copy files from Linux to Windows box with WinSCP/SCP OR 
```bash
$ tar -czvf perf_cpu-ctf.tar.gz perf*
```
- (Optional if you want absolute timestamps) Place timestamp.txt next to perf.data.txt
- Open perf.data.txt with WPA
- For the perf CTF file (optional) On Windows, Zip the folder up and rename to .ctf extension. E.g. perf_cpu-ctf.ctf (which is really a .zip file)
  - CTF (Optional) Kallsyms needs to be on your Desktop


# Presentations

If you want to see a demo or get more in-depth info on using these tools check out a talk given at the [Linux Tracing Summit](https://www.tracingsummit.org/ts/2019/):
>Linux & Windows Perf Analysis using WPA, ([slides](https://www.tracingsummit.org/ts/2019/files/Tracingsummit2019-wpa-berg-gibeau.pdf)) ([video](https://youtu.be/HUbVaIi-aaw))