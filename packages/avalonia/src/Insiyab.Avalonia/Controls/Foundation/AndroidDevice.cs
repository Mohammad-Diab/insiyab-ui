using System.Reflection;
using System.Runtime.InteropServices;

namespace Insiyab.Avalonia.Controls;

/// <summary>What the library reads from an Android device without referencing Android: the animator duration scale, through Mono.Android by name, and whether the device calls itself low on memory, from a system property.</summary>
internal static unsafe class AndroidDevice
{
    // Under this much memory (a phone sold with 3 GB or less; one sold with 4 GB reports about 3.5 to 3.8) a device starts with light effects even when it does not call itself low on memory.
    internal const long SmallMemory = 3584L * 1024 * 1024;

    // Tests stand in for the device.
    internal static Func<float?>? AnimatorScaleSource { get; set; }
    internal static Func<bool>? LowMemorySource { get; set; }

    /// <summary>The system's animator duration scale (0 when "Remove animations" is on); null off Android or when it cannot be read.</summary>
    public static float? AnimatorScale => AnimatorScaleSource is { } source ? source() : Os.IsAndroid ? ReadAnimatorScale() : null;

    /// <summary>True on an Android device that reports itself low on memory, or has under 3.5 GB.</summary>
    public static bool IsLowMemory => LowMemorySource is { } source ? source() : Os.IsAndroid && ReadLowMemory();

    // Every name is a constant, so a trimmed app keeps the members read here.
    private static float? ReadAnimatorScale()
    {
        try
        {
            var application = Type.GetType("Android.App.Application, Mono.Android");
            var context = Type.GetType("Android.Content.Context, Mono.Android");
            var resolver = Type.GetType("Android.Content.ContentResolver, Mono.Android");
            var global = Type.GetType("Android.Provider.Settings+Global, Mono.Android");
            if (application is null || context is null || resolver is null || global is null) return null;
            var app = application.GetProperty("Context", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
            var content = app is null ? null : context.GetProperty("ContentResolver", BindingFlags.Public | BindingFlags.Instance)?.GetValue(app);
            if (content is null) return null;
            var read = global.GetMethod("GetFloat", BindingFlags.Public | BindingFlags.Static, [resolver, typeof(string), typeof(float)]);
            return read?.Invoke(null, [content, "animator_duration_scale", 1f]) is float scale ? scale : null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static bool ReadLowMemory()
    {
        try
        {
            if (Property("ro.config.low_ram") == "true") return true;
            var total = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
            return total > 0 && total < SmallMemory;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static string? Property(string name)
    {
        var key = System.Text.Encoding.ASCII.GetBytes(name + "\0");
        var value = stackalloc byte[92];
        fixed (byte* k = key)
        {
            var length = SystemPropertyGet(k, value);
            return length > 0 ? Marshal.PtrToStringAnsi((nint)value, length) : null;
        }
    }

    [DllImport("libc", EntryPoint = "__system_property_get")]
    private static extern int SystemPropertyGet(byte* name, byte* value);
}
