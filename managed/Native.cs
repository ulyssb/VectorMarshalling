using System.Runtime.InteropServices;

namespace VectorMarshalling;

// Raw P/Invoke surface. Nothing here does any conversion - it's the
// literal signature of the native functions in native/interop.cpp.
internal static partial class Native
{
    private const string Lib = "interop"; // resolves to libinterop.dylib on macOS

    [StructLayout(LayoutKind.Sequential)]
    public struct Point3D
    {
        public double X, Y, Z;
    }

    [LibraryImport(Lib)]
    public static partial int ComputeDoubleVector(double[] buffer, int capacity);

    [DllImport(Lib)]
    public static extern int ComputeDoubleVector2(double[] buffer, int capacity);

    [LibraryImport(Lib)]
    public static partial IntPtr ComputePointVector(out int outLength);

    [LibraryImport(Lib)]
    public static partial void FreePointVector(IntPtr ptr);

    [LibraryImport(Lib)]
    public static partial IntPtr ComputeStringVector(out int outLength);

    [DllImport(Lib)]
    public static extern IntPtr ComputeStringVector2(out int outLength);

    [LibraryImport(Lib)]
    public static partial void FreeStringVector(IntPtr arr, int length);

    [LibraryImport(Lib)]
    public static partial int ComputeIntDoubleMap(int[] outKeys, double[] outValues, int capacity);

    [LibraryImport(Lib)]
    public static partial int ComputeStringIntMap(out IntPtr outKeys, out IntPtr outValues);

    [LibraryImport(Lib)]
    public static partial void FreeStringIntMap(IntPtr keys, IntPtr values, int length);
}
