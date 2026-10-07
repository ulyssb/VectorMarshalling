// Minimal native library demonstrating C++ -> C# interop patterns for
// std::vector and std::map/std::unordered_map via P/Invoke.
//
// Key rule: std::vector / std::map / std::unordered_map are NEVER passed
// across the ABI directly - their layout is implementation-defined. Every
// function here flattens the container into a C-compatible shape
// (raw pointer + length, or parallel arrays) before crossing the boundary.

#include <cstdint>
#include <cstring>
#include <string>
#include <vector>
#include <map>
#include <unordered_map>
#include <algorithm>

#define EXPORT extern "C" __attribute__((visibility("default")))

// ---------------------------------------------------------------------
// 1) vector<double> - blittable elements, caller-allocated buffer.
//    Cheapest possible pattern: one memcpy, no native allocation,
//    no free function needed since C# owns the buffer.
// ---------------------------------------------------------------------
EXPORT int32_t ComputeDoubleVector(double* buffer, int32_t capacity) {
    std::vector<double> result = {1.1, 2.2, 3.3, 4.4, 5.5, 6.6};
    if (capacity < 0) capacity = 0; // a negative n would become a huge size_t in memcpy
    int32_t n = std::min<int32_t>(capacity, static_cast<int32_t>(result.size()));
    std::memcpy(buffer, result.data(), n * sizeof(double));
    return static_cast<int32_t>(result.size()); // true size, in case buffer was too small
}

// Identical to ComputeDoubleVector; bound with [DllImport] on the C# side
// so the benchmark can compare it against the [LibraryImport] stub.
EXPORT int32_t ComputeDoubleVector2(double* buffer, int32_t capacity) {
    std::vector<double> result = {1.1, 2.2, 3.3, 4.4, 5.5, 6.6};
    if (capacity < 0) capacity = 0; // a negative n would become a huge size_t in memcpy
    int32_t n = std::min<int32_t>(capacity, static_cast<int32_t>(result.size()));
    std::memcpy(buffer, result.data(), n * sizeof(double));
    return static_cast<int32_t>(result.size()); // true size, in case buffer was too small
}

// ---------------------------------------------------------------------
// 2) vector<Point3D> - blittable POD struct, native-allocated.
//    Still a single memcpy's worth of work, but now C++ owns the memory
//    so C# must call the matching Free function.
// ---------------------------------------------------------------------
struct Point3D { double x, y, z; };
static_assert(sizeof(Point3D) == 3 * sizeof(double), "must match C# Point3D layout");

EXPORT Point3D* ComputePointVector(int32_t* outLength) {
    std::vector<Point3D> result = { {1, 2, 3}, {4, 5, 6}, {7, 8, 9} };
    *outLength = static_cast<int32_t>(result.size());
    Point3D* buffer = new Point3D[result.size()];
    std::memcpy(buffer, result.data(), result.size() * sizeof(Point3D));
    return buffer;
}

EXPORT void FreePointVector(Point3D* ptr) {
    delete[] ptr;
}

// ---------------------------------------------------------------------
// 3) vector<std::string> - non-blittable elements.
//    Each string must be copied out individually into a heap-allocated
//    C string; C# then does N separate PtrToStringAnsi calls. This is
//    the slow path referenced in the perf comparison in Program.cs.
// ---------------------------------------------------------------------
EXPORT char** ComputeStringVector(int32_t* outLength) {
    std::vector<std::string> result;
    for (int32_t i = 0; i < 100; ++i) result.push_back("str" + std::to_string(i));
    *outLength = static_cast<int32_t>(result.size());
    char** arr = new char*[result.size()];
    for (size_t i = 0; i < result.size(); ++i) {
        arr[i] = new char[result[i].size() + 1];
        std::memcpy(arr[i], result[i].c_str(), result[i].size() + 1);
    }
    return arr;
}

// Identical to ComputeStringVector; bound with [DllImport] for the benchmark.
EXPORT char** ComputeStringVector2(int32_t* outLength) {
    std::vector<std::string> result;
    for (int32_t i = 0; i < 100; ++i) result.push_back("str" + std::to_string(i));
    *outLength = static_cast<int32_t>(result.size());
    char** arr = new char*[result.size()];
    for (size_t i = 0; i < result.size(); ++i) {
        arr[i] = new char[result[i].size() + 1];
        std::memcpy(arr[i], result[i].c_str(), result[i].size() + 1);
    }
    return arr;
}


EXPORT void FreeStringVector(char** arr, int32_t length) {
    for (int32_t i = 0; i < length; ++i) delete[] arr[i];
    delete[] arr;
}

// ---------------------------------------------------------------------
// 4) unordered_map<int32_t, double> - blittable key AND value.
//    Flattened into two parallel caller-allocated arrays - same cost
//    profile as case 1, just two memcpy-able buffers instead of one.
// ---------------------------------------------------------------------
EXPORT int32_t ComputeIntDoubleMap(int32_t* outKeys, double* outValues, int32_t capacity) {
    std::unordered_map<int32_t, double> result = {
        {1, 1.5}, {2, 2.5}, {3, 3.5}, {4, 4.5}
    };
    int32_t n = 0;
    for (const auto& [k, v] : result) {
        if (n >= capacity) break;
        outKeys[n] = k;
        outValues[n] = v;
        ++n;
    }
    return static_cast<int32_t>(result.size());
}

// ---------------------------------------------------------------------
// 5) map<std::string, int32_t> - non-blittable key (string), blittable
//    value. std::map keeps keys sorted, so iteration order here is
//    alphabetical - that ordering survives into the C# Dictionary's
//    insertion order (Dictionary doesn't guarantee it, but in practice
//    preserves insertion order absent removals).
// ---------------------------------------------------------------------
EXPORT int32_t ComputeStringIntMap(char*** outKeys, int32_t** outValues) {
    std::map<std::string, int32_t> result = {
        {"cherry", 30}, {"apple", 10}, {"banana", 20}
    };
    int32_t n = static_cast<int32_t>(result.size());
    char** keys = new char*[n];
    int32_t* values = new int32_t[n];
    int32_t i = 0;
    for (const auto& [k, v] : result) {
        keys[i] = new char[k.size() + 1];
        std::memcpy(keys[i], k.c_str(), k.size() + 1);
        values[i] = v;
        ++i;
    }
    *outKeys = keys;
    *outValues = values;
    return n;
}

EXPORT void FreeStringIntMap(char** keys, int32_t* values, int32_t length) {
    for (int32_t i = 0; i < length; ++i) delete[] keys[i];
    delete[] keys;
    delete[] values;
}
