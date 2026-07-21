using System.Runtime.InteropServices;

namespace ZGF.WebView.Cef;

/// <summary>
/// Locates the CEF native payload and the <c>zgf-cef-helper</c> subprocess relative to the running
/// app. The CefGlue runtime packages copy <c>libcef</c> + the <c>.pak</c> resources into the app root
/// on Windows (and under <c>runtimes/&lt;rid&gt;/native</c> otherwise), and the web-view targets stage
/// the helper into <c>cef-helper/</c>. Keeping this resolution behind one type is also the seam where a
/// future "download CEF on first run" would slot in (plan decision 3) without touching call sites.
/// </summary>
internal static class CefPaths
{
    private static readonly bool IsWindows =
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    public static string BaseDir => AppContext.BaseDirectory;

    public static string HelperExe =>
        Path.Combine(BaseDir, "cef-helper", IsWindows ? "zgf-cef-helper.exe" : "zgf-cef-helper");

    /// <summary>Directory that contains <c>libcef</c> — the app root when the runtime package flattened
    /// it there, else the per-RID native folder.</summary>
    public static string LibCefDir => FindDirContaining(LibCefFileName);

    /// <summary>Directory that contains the <c>.pak</c> resources (usually the same as
    /// <see cref="LibCefDir"/>).</summary>
    public static string ResourcesDir => FindDirContaining("icudtl.dat", "resources.pak");

    /// <summary>Directory that holds the locale <c>.pak</c> files (a <c>locales</c> subfolder of the
    /// resources dir, or the per-RID <c>native/locales</c> the Windows targets create).</summary>
    public static string LocalesDir
    {
        get
        {
            var candidates = new[]
            {
                Path.Combine(ResourcesDir, "locales"),
                Path.Combine(BaseDir, "runtimes", Rid, "native", "locales"),
                Path.Combine(BaseDir, "locales"),
            };
            foreach (var c in candidates)
                if (Directory.Exists(c))
                    return c;
            return Path.Combine(ResourcesDir, "locales");
        }
    }

    /// <summary>Root under which all CEF profile data lives, kept out of the source tree so
    /// cookies/localStorage survive restarts (plan decision 7).</summary>
    public static string CacheRoot =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ZGF", "WebViewCache");

    /// <summary>The per-profile cache directory (a subdirectory of <see cref="CacheRoot"/>). Setting
    /// <c>CefSettings.CachePath</c> to this is what keeps CEF's GPU/Dawn shader caches out of the
    /// process working directory — without it the GPU process drops <c>GPUCache/</c>,
    /// <c>DawnCache/</c> next to wherever the app was launched.</summary>
    public static string ProfileCachePath => Path.Combine(CacheRoot, "Default");

    private static string LibCefFileName =>
        IsWindows ? "libcef.dll" :
        RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "libcef.dylib" : "libcef.so";

    private static string Rid =>
        (RuntimeInformation.OSArchitecture == Architecture.Arm64, IsWindows,
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) switch
        {
            (true, true, _) => "win-arm64",
            (false, true, _) => "win-x64",
            (true, _, true) => "osx-arm64",
            (false, _, true) => "osx-x64",
            (true, _, _) => "linux-arm64",
            _ => "linux-x64",
        };

    private static string FindDirContaining(params string[] anyOf)
    {
        var roots = new[]
        {
            BaseDir,
            Path.Combine(BaseDir, "runtimes", Rid, "native"),
        };
        foreach (var root in roots)
            foreach (var f in anyOf)
                if (File.Exists(Path.Combine(root, f)))
                    return root;
        // Fall back to the app root; CEF then reports a clear "could not load" error if it is wrong.
        return BaseDir;
    }
}
