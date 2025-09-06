# bigRIG
Repo for remote interopability generation tool

This project will be developed over time, but the general concept has already been proved elsewhere. This is a clean rewrite of that tool using pure C# and is intended to be cross-platform.

What is it? Good question. In a nutshell, it is a tool to generate a "server" and "client" that is either cross-process, cross-language, or cross-machine.

Admittedly, tools already exist that do this. The unique capability this tool adds is the ability to define the interface in the same language as the server. On top of that, the tool then builds the boiler-plate code that allows interacting with that interface as if it was written natively in the target language.

Take gRPC as an example, a technology this tool builds on top of. While gRPC is cross-process, cross-language, and cross-machine, it is defined in a domain-specific language: protobuf. This in itself would not be too big a deal. However, once that protobuf has been defined, a gRPC server must still be written. Additionally, clients must interact with proto directly and gRPC APIs.

While all of this is possible, it becomes tedious and ties the code to a very specific gRPC pattern. However, gRPC and protobuf are just technologies that support creating a cross-language, cross-process system. ZMQ and flatbuffers are also equivalent technologies, and to use them should not require a complete rewrite of the software.

# Goals
* Generator code written in C#
* Cross-platform, supporting both Windows and Linux (other platforms may also work)
* Abstract the underlying technologies enabling the system
* Support for multiple language servers and multiple language clients
* Support for consuming extensions/plugins to extend supported languages/types
* Support object-oriented programming
* Support patterns to optimize performance without requiring significant developer work to use

# Usage
bigRIG operates in two distinct steps:

1. Parsing of the original server interface definition into JSON
2. Generation of the boilerplate code using the JSON

See command line help for help:
```bash
RigGen --help
# Generate JSON
RigGen genjson --directory patha --outpath pathb -- additional args
# Generate boilerplate code
RigGen gencode --client_language Cpp --client_language CSharp --server_language Cpp --outpath patha
```

# Further Documentation
See the Documentation folder for further documentation about design, capabilities, and other information about the tool.
