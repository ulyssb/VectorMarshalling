# VectorMarshalling

A small playground for passing C++ standard containers (`std::vector`, `std::map`,
`std::unordered_map`) across the native/managed boundary into C#, and comparing
the cost of the different marshalling strategies.

## Goal

Show, side by side, the common patterns for getting container data out of C++ and into
.NET collections, and how blittable(same in) vs non-blittable element types affect performance:

| C++ type                           | C# type                    | Strategy                                                        |
|------------------------------------|----------------------------|-----------------------------------------------------------------|
| `std::vector<double>`              | `double[]`                 | Caller-allocated buffer, no native allocation or free           |
| `std::vector<Point3D>`             | `Point3D[]`                | Native-allocated struct array, bulk copy via `Span<T>`, then free |
| `std::vector<std::string>`         | `string[]`                 | Native-allocated `char**`, one `PtrToStringAnsi` per element     |
| `std::unordered_map<int, double>`  | `Dictionary<int, double>`  | Two parallel caller-allocated arrays (keys / values)            |
| `std::map<std::string, int>`       | `Dictionary<string, int>`  | Native-allocated key/value arrays, string keys marshalled per element |

The program also runs a rough benchmark comparing `[LibraryImport]` (source-generated)
with `[DllImport]` (runtime-generated) stubs, and blittable vs string fetches.

## Layout

```
native/
  interop.cpp      C++ side: builds the containers and exports them through a C ABI
  build.sh         Compiles interop.cpp into libinterop.dylib
managed/
  Native.cs        Raw P/Invoke declarations, no conversion logic
  VectorExamples.cs, MapExamples.cs   Conversion from native buffers to .NET types
  Program.cs       Runs each example and the benchmark
```

## Running

Requirements: macOS, `clang++` (Xcode command line tools), and the .NET 10 SDK.

```sh
cd managed
dotnet run -c Release
```

The `.csproj` runs `native/build.sh` automatically before each build, so you don't need to
build the native library separately. It is only recompiled when `interop.cpp` changes.
Use `-c Release` when reading the benchmark numbers.

`build.sh` doesn't pin a macOS SDK: it lets `clang++` pick the default one (or uses `SDKROOT`
if you set it). If linking against the default SDK fails, which happens when a Command Line
Tools update installed an SDK newer than its linker, it retries with the other installed SDKs,
newest first, and prints a hint to update the Command Line Tools.

> The build script currently targets macOS only (`.dylib`). On Linux or Windows you would
> need to adapt `build.sh` to produce `libinterop.so` / `interop.dll` and update the
> `.csproj` accordingly.
