using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using FreeTypeSharp;

namespace ZGF.Gui.Web.Native;

/// Makes FreeTypeSharp's <c>[DllImport("freetype")]</c> calls resolve to our
/// statically-linked <c>libfreetype.a</c> (NativeFileReference) under browser-wasm.
///
/// FreeTypeSharp's <c>FT</c> type initializer registers a <c>DllImportResolver</c>
/// that throws <see cref="PlatformNotSupportedException"/> on any OS that isn't
/// Windows/macOS/Linux/Android — so on "browser" the first FreeType call fails
/// before our static library is consulted. The supported pattern for a statically
/// linked native lib is a resolver returning <see cref="NativeLibrary.GetMainProgramHandle"/>,
/// but <see cref="NativeLibrary.SetDllImportResolver"/> is set-once and FreeTypeSharp
/// already claimed it. So we force its initializer to run, then replace its entry in
/// the runtime's internal resolver table (a <see cref="ConditionalWeakTable{TKey,TValue}"/>
/// keyed by assembly, read by NativeLibrary.LoadLibraryCallbackStub on every load).
[SupportedOSPlatform("browser")]
internal static class WasmFreeTypeResolver
{
    // With "freetype" registered as a wasm pinvoke module (see ZGF.Gui.Web.csproj),
    // FreeType calls bind statically and this resolver is never consulted. It stays
    // as a safety net only: return IntPtr.Zero to defer to default resolution rather
    // than let FreeTypeSharp's own resolver throw PlatformNotSupportedException.
    private static IntPtr Resolve(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
        => IntPtr.Zero;

    [DynamicDependency("s_nativeDllResolveMap", typeof(NativeLibrary))]
    public static void Install()
    {
        // Run FreeTypeSharp's FT cctor so it registers its (throwing) resolver first.
        RuntimeHelpers.RunClassConstructor(typeof(FT).TypeHandle);

        var ftAssembly = typeof(FT).Assembly;
        DllImportResolver ours = Resolve;

        var field = typeof(NativeLibrary).GetField("s_nativeDllResolveMap",
            BindingFlags.NonPublic | BindingFlags.Static);

        if (field?.GetValue(null) is ConditionalWeakTable<Assembly, DllImportResolver> map)
        {
            map.AddOrUpdate(ftAssembly, ours);
            var ok = map.TryGetValue(ftAssembly, out var current) && ReferenceEquals(current, ours);
            Console.WriteLine($"[wasm-freetype] resolver override installed: {ok}");
            return;
        }

        Console.WriteLine("[wasm-freetype] resolver map not found; trying direct set");
        try { NativeLibrary.SetDllImportResolver(ftAssembly, ours); }
        catch (InvalidOperationException) { Console.WriteLine("[wasm-freetype] direct set failed (already set)"); }
    }
}
