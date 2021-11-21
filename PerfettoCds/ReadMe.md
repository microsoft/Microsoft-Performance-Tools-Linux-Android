# WPA Perfetto Plugin

The PerfettoCds project adds a pipeline for displaying data from Perfetto traces using Windows Performance Toolkit SDK. The PerfettoProcessor project contains the logic for processing Perfetto traces.

Data is gathered from the Perfetto trace through trace_processor_shell.exe. Trace_processor_shell.exe opens an interface that allows for making SQL queries over HTTP/RPC to localhost. All the trace data is retrieved through SQL queries and is processed through the data cooker pipeline. 

See the [Architecture](Architecture.md) document for more information.

## Trace config files
See Perfetto documentation for more information on how to create [config files](https://perfetto.dev/docs/concepts/config) and [collect traces](https://perfetto.dev/docs/quickstart/android-tracing). 

See the ["Record new trace"](https://ui.perfetto.dev/#!/record) menu for a helpful guide on creating custom config files.

### Some config options for some event types we support
* Generic Events
  * `data_sources: {
  config {
    name: "My.Trace.Event"
  }
}`
* CPU Counters (coarse)
  * `data_sources: {
    config {
        name: "linux.sys_stats"
        sys_stats_config {
            stat_period_ms: 1000
            stat_counters: STAT_CPU_TIMES
            stat_counters: STAT_FORK_COUNT
        }
    }
}`
* CPU Frequency Scaling
  * `data_sources: {
    config {
        name: "linux.ftrace"
        ftrace_config {
            ftrace_events: "power/cpu_frequency"
            ftrace_events: "power/cpu_idle"
            ftrace_events: "power/suspend_resume"
        }
    }
}`
* CPU Scheduler
  * `data_sources: {
    config {
        name: "linux.process_stats"
        target_buffer: 1
        process_stats_config {
            scan_all_processes_on_start: true
        }
    }
}
data_sources: {
    config {
        name: "linux.ftrace"
        ftrace_config {
            ftrace_events: "sched/sched_switch"
            ftrace_events: "power/suspend_resume"
            ftrace_events: "sched/sched_wakeup"
            ftrace_events: "sched/sched_wakeup_new"
            ftrace_events: "sched/sched_waking"
            ftrace_events: "sched/sched_process_exit"
            ftrace_events: "sched/sched_process_free"
            ftrace_events: "task/task_newtask"
            ftrace_events: "task/task_rename"
        }
    }
}`
* Perfetto Process Memory
  * `data_sources: {
    config {
        name: "linux.process_stats"
        target_buffer: 1
        process_stats_config {
            proc_stats_poll_ms: 1000
        }
    }
}`
* Perfetto System Memory
  * `data_sources: {
    config {
        name: "linux.sys_stats"
        sys_stats_config {
            meminfo_period_ms: 1000
            meminfo_counters: MEMINFO_ACTIVE
            meminfo_counters: MEMINFO_ACTIVE_FILE
            meminfo_counters: MEMINFO_CACHED
        }
    }
}`


## Additional Features

### Provider-GUID Mappings
Perfetto does not surface the name of an event's provider. As a workaround, the Perfetto plugin has support to load a ProviderGuid XML mapping file that maps GUIDs to Provider Names. Using this mapping, the plugin will search each GenericEvent's debug annotations for a specific key that holds a GUID value, and it will convert that GUID to the ProviderName in the mapping. The GenericEvent table will then display an extra column with that Provider name.

#### To use:

1. Your GenericEvents should be instrumented to output debug annotations where the argument key is the string "ProviderGuid" and the argument value is the provider GUID string.
2. Add an XML file with the exact name "ProviderMapping.xml" to the same directory that contains the Perfetto plugin binaries. That directory is the one containing "PerfettoCds.dll". The XML schema should look like the following. The "DebugAnnotationKey" attribute is optional. That specifies which debug annotation key to look for. It defaults to "ProviderGuid". Add an EventProvider node for each Provider you want mapped.

        <?xml version="1.0" encoding="utf-8"?>
        <EventProviders DebugAnnotationKey="MyCustomDebugAnnotationKey">
        <EventProvider Id="My.Trace.Event1" Name="e6cc2436-d77f-495d-96ce-4c46ddc569a0" />
        <EventProvider Id="My.Trace.Event2" Name="a30f819e-db13-4523-b578-1f1a0b4d6533" />
        </EventProviders>