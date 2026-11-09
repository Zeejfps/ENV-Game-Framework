using System.Runtime.InteropServices;

namespace ZGF.Fonts;

/// <summary>
/// Keeps the FreeType and HarfBuzz that ship with this library bound to themselves on Linux.
/// </summary>
/// <remarks>
/// <para>
/// FreeTypeSharp and HarfBuzzSharp ship their own <c>libfreetype.so</c> and
/// <c>libHarfBuzzSharp.so</c>, which export every <c>FT_*</c> and <c>hb_*</c> function under the
/// same names as the system's <c>libfreetype.so.6</c> and <c>libharfbuzz.so.0</c>. .NET loads them
/// with lazy binding, so each of their calls into themselves is resolved on first use, and the
/// dynamic linker looks in the process's global scope before the library's own. Once anything puts
/// the system copies there — Chromium does, as libcef links Pango and loads GTK — HarfBuzz's own
/// calls land in the system HarfBuzz, which reads the bundled one's structures as its own and
/// jumps through a garbage pointer, crashing the app the first time it shapes text after a web view
/// starts.
/// </para>
/// <para>
/// Loading each library first with <c>RTLD_DEEPBIND</c> makes it look in itself before the global
/// scope, and <c>RTLD_NOW</c> binds every call at once. The library's own import, made later by
/// .NET from the same file, gets that already-loaded copy back from the dynamic linker. Both flags
/// are glibc's; where there is no glibc (musl) the libraries are left to .NET as before.
/// </para>
/// </remarks>
internal static class NativeFontLibraries
{
    private const int RTLD_NOW = 0x2;
    private const int RTLD_DEEPBIND = 0x8;

    private static readonly string[] LibraryFiles = ["libfreetype.so", "libHarfBuzzSharp.so"];

    // Loading is process-wide and happens once; the field records only that it was attempted.
    private static readonly bool Attempted = OperatingSystem.IsLinux() && LoadIsolated();

    /// <summary>Loads the libraries isolated, once, before anything calls into them. Call it ahead
    /// of the first FreeType or HarfBuzz call.</summary>
    public static void EnsureLoaded() => _ = Attempted;

    private static bool LoadIsolated()
    {
        try
        {
            foreach (var file in LibraryFiles)
            {
                var path = FindNativeFile(file);
                if (path == null) continue;
                if (dlopen(path, RTLD_NOW | RTLD_DEEPBIND) == IntPtr.Zero)
                    Console.WriteLine($"[Fonts] Could not load {path} isolated: {Marshal.PtrToStringUTF8(dlerror())}");
            }
        }
        catch (DllNotFoundException)
        {
            // No glibc libdl (musl): no RTLD_DEEPBIND either.
        }
        return true;
    }

    /// <summary>
    /// <paramref name="file"/> in the first of the host's native search directories holding it, the
    /// directories and order .NET itself probes, so the file loaded here is the one .NET loads.
    /// </summary>
    private static string? FindNativeFile(string file)
    {
        if (AppContext.GetData("NATIVE_DLL_SEARCH_DIRECTORIES") is not string directories)
            return null;

        foreach (var directory in directories.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            var path = Path.Combine(directory, file);
            if (File.Exists(path)) return path;
        }
        return null;
    }

    [DllImport("libdl.so.2")]
    private static extern IntPtr dlopen([MarshalAs(UnmanagedType.LPUTF8Str)] string path, int flags);

    [DllImport("libdl.so.2")]
    private static extern IntPtr dlerror();
}
