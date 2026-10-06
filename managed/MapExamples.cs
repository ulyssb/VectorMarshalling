using System.Runtime.InteropServices;

namespace VectorMarshalling;

internal static class MapExamples
{
    // std::unordered_map<int32_t, double> -> Dictionary<int, double>
    // Blittable key AND value: two parallel caller-allocated arrays,
    // each filled with a tight native loop, then zipped into a Dictionary.
    // No native allocation, no free call needed.
    public static Dictionary<int, double> ComputeIntDoubleMap()
    {
        var keys = new int[32];
        var values = new double[32];
        int actual = Native.ComputeIntDoubleMap(keys, values, keys.Length);
        // Native returns the true size even when it stopped at `capacity`,
        // so retry with exact-size arrays rather than reading past the end.
        while (actual > keys.Length)
        {
            keys = new int[actual];
            values = new double[actual];
            actual = Native.ComputeIntDoubleMap(keys, values, keys.Length);
        }

        var dict = new Dictionary<int, double>(actual);
        for (int i = 0; i < actual; i++)
            dict[keys[i]] = values[i];
        return dict;
    }

    // std::map<std::string, int32_t> -> Dictionary<string, int>
    // Non-blittable key (string), blittable value: two Marshal.Copy calls to
    // pull the pointer/value arrays over, then N PtrToStringAnsi calls for
    // the keys - the same per-element cost as the string vector case, just
    // applied only to the key half of the pair.
    public static Dictionary<string, int> ComputeStringIntMap()
    {
        int count = Native.ComputeStringIntMap(out IntPtr keysPtr, out IntPtr valuesPtr);
        try
        {
            var keyPtrs = new IntPtr[count];
            var values = new int[count];
            Marshal.Copy(keysPtr, keyPtrs, 0, count);
            Marshal.Copy(valuesPtr, values, 0, count);

            var dict = new Dictionary<string, int>(count);
            for (int i = 0; i < count; i++)
            {
                string key = Marshal.PtrToStringAnsi(keyPtrs[i]) ?? string.Empty;
                dict[key] = values[i];
            }
            return dict;
        }
        finally
        {
            Native.FreeStringIntMap(keysPtr, valuesPtr, count);
        }
    }
}
