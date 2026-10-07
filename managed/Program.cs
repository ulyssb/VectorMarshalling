using System.Diagnostics;
using VectorMarshalling;

Console.WriteLine("=== vector<double> -> double[]  (blittable, caller-allocated) ===");
Console.WriteLine(string.Join(", ", VectorExamples.ComputeDoubles()));

Console.WriteLine("\n=== vector<Point3D> -> Point3D[]  (blittable struct, native-allocated) ===");
foreach (var p in VectorExamples.ComputePoints())
    Console.WriteLine($"  ({p.X}, {p.Y}, {p.Z})");

Console.WriteLine("\n=== vector<std::string> -> string[]  (non-blittable, native-allocated) ===");
Console.WriteLine(string.Join(", ", VectorExamples.ComputeStrings()));

Console.WriteLine("\n=== unordered_map<int, double> -> Dictionary<int, double> ===");
foreach (var kv in MapExamples.ComputeIntDoubleMap())
    Console.WriteLine($"  {kv.Key} -> {kv.Value}");

Console.WriteLine("\n=== map<string, int> -> Dictionary<string, int>  (ordered on the C++ side) ===");
foreach (var kv in MapExamples.ComputeStringIntMap())
    Console.WriteLine($"  {kv.Key} -> {kv.Value}");

Console.WriteLine("\n=== rough perf: blittable double[] fetch vs non-blittable string[] fetch ===");
const int iterations = 200_000;

// warm up JIT / native lib before timing
VectorExamples.ComputeDoubles();
VectorExamples.ComputeDoubles2();
VectorExamples.ComputeStrings();
VectorExamples.ComputeStrings2();

var sw = Stopwatch.StartNew();
for (int i = 0; i < iterations; i++) VectorExamples.ComputeDoubles();
sw.Stop();
Console.WriteLine($"double[6]  x{iterations}: {sw.ElapsedMilliseconds} ms total, {sw.Elapsed.TotalMicroseconds / iterations:F3} us/call");

sw.Restart();
for (int i = 0; i < iterations; i++) VectorExamples.ComputeDoubles2();
sw.Stop();
Console.WriteLine($"double[6] DLLImport  x{iterations}: {sw.ElapsedMilliseconds} ms total, {sw.Elapsed.TotalMicroseconds / iterations:F3} us/call");

sw.Restart();
for (int i = 0; i < iterations; i++) VectorExamples.ComputeStrings();
sw.Stop();
Console.WriteLine($"string[100]  x{iterations}: {sw.ElapsedMilliseconds} ms total, {sw.Elapsed.TotalMicroseconds / iterations:F3} us/call");

sw.Restart();
for (int i = 0; i < iterations; i++) VectorExamples.ComputeStrings2();
sw.Stop();
Console.WriteLine($"string[100]  DLLImport x{iterations}: {sw.ElapsedMilliseconds} ms total, {sw.Elapsed.TotalMicroseconds / iterations:F3} us/call");