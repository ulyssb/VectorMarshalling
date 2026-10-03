using System.Runtime.InteropServices;

namespace VectorMarshalling;

internal static class VectorExamples
{
    // std::vector<double> -> double[]
    // Caller-allocated buffer: no native allocation, no free call, just a memcpy.
    public static double[] ComputeDoubles()
    {
        var buffer = new double[16];
        int actual = Native.ComputeDoubleVector(buffer, buffer.Length);
        Array.Resize(ref buffer, actual);
        return buffer;
    }

    public static double[] ComputeDoubles2()
    {
        var buffer = new double[16];
        int actual = Native.ComputeDoubleVector2(buffer, buffer.Length);
        Array.Resize(ref buffer, actual);
        return buffer;
    }

    // std::vector<Point3D> -> Point3D[]
    // Native-allocated blittable struct array: zero-copy view via Span<T>,
    // then a single bulk copy into managed memory, then free the native buffer.
    public static Native.Point3D[] ComputePoints()
    {
        IntPtr ptr = Native.ComputePointVector(out int len);
        try
        {
            var result = new Native.Point3D[len];
            unsafe
            {
                new ReadOnlySpan<Native.Point3D>((void*)ptr, len).CopyTo(result);
            }
            return result;
        }
        finally
        {
            Native.FreePointVector(ptr);
        }
    }

    // std::vector<std::string> -> string[]
    // Non-blittable: one Marshal.Copy for the pointer array, then N separate
    // PtrToStringAnsi calls (N allocations + N strlen scans). This is the
    // pattern that shows up as the slow line in the benchmark in Program.cs.
    public static string[] ComputeStrings()
    {
        IntPtr arr = Native.ComputeStringVector(out int len);
        try
        {
            var ptrs = new IntPtr[len];
            Marshal.Copy(arr, ptrs, 0, len);

            var result = new string[len];
            for (int i = 0; i < len; i++)
                result[i] = Marshal.PtrToStringAnsi(ptrs[i]) ?? string.Empty;

            return result;
        }
        finally
        {
            Native.FreeStringVector(arr, len);
        }
    }

    public static string[] ComputeStrings2()
    {
        IntPtr arr = Native.ComputeStringVector2(out int len);
        try
        {
            var ptrs = new IntPtr[len];
            Marshal.Copy(arr, ptrs, 0, len);

            var result = new string[len];
            for (int i = 0; i < len; i++)
                result[i] = Marshal.PtrToStringAnsi(ptrs[i]) ?? string.Empty;

            return result;
        }
        finally
        {
            Native.FreeStringVector(arr, len);
        }
    }

}
