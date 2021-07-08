# WPA Perfetto Plugin

The PerfettoCds project adds a pipeline for displaying data from Perfetto traces using Windows Performance Toolkit SDK. The PerfettoProcessor project contains the logic for processing Perfetto traces.

Data is gathered from the Perfetto trace through trace_processor_shell.exe. Trace_processor_shell.exe opens an interface that allows for making SQL queries over HTTP/RPC to localhost. All the trace data is retrieved with SQL queries. Data is serialized over the HTTP interface with Protobuf objects. The original TraceProcessor protobuf object (trace_processor.proto) and the C# conversions are included in the project.

PerfettoSourceParser will start the trace_processor_shell.exe process. SQL Queries will be made over HTTP and the protobuf output will be converted to objects of type PerfettoSqlEvent. One query will be made per SQL table. Each query will produce events of the same type and will be processed by their own individual source cooker. For a complete generic event, we need data from 5 tables: slice, args, thread_track, thread, and process. A composite cooker will then take all the data gathered from each individual source cooker and join them to create the final generic event.

For example, for the slice table, the process goes like this:

1. Perform a SQL query into the Perfetto trace through trace_processor_shell.exe ("select * from slice"). This returns a QueryResult protobuf object. The QueryResult object is parsed and objects of type PerfettoSliceEvent are returned. One event for each row in the SQL table.
2. PerfettoSliceCooker will process/store all the PerfettoSliceEvents.
3. Once all the other tables have also finished, PerfettoGenericEventCooker will gather all the events from each cooker and do a join on them to create complete PerfettoGenericEvent objects.
4. Create WPA table of PerfettoGenericEvents

## Trace Processor Shell
Trace_processor_shell.exe is built from the [Perfetto GitHub repo](https://github.com/google/perfetto). To build this for Windows, follow the instructions from their [documentation](https://perfetto.dev/docs/contributing/build-instructions#building-on-windows) 