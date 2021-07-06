TraceProcessor.cs and Descriptor.cs are C# equivalent versions of traceprocessor.proto and descriptor.proto. 
The .proto files are from the Perfetto repo (https://github.com/google/perfetto). To convert .proto to a C# class,
the protoc.exe executable can be used, which can be retrieved from the Google.Protobuf.Tools Nuget package.

The command lines to generate the C# files are something like this:

protoc.exe -I d:\perfetto_root --csharp_out=output_dir D:\perfetto_root\protos\perfetto\trace_processor\trace_processor.proto
protoc.exe -I d:\perfetto_root --csharp_out=output_dir D:\perfetto_root\protos\perfetto\common\descriptor.proto