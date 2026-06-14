using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using FreeTypeSharp;

namespace ZGF.Gui.Web.Native;

/// Lets FreeTypeSharp's [DllImport("freetype")] calls reach our statically-linked
/// libfreetype.a under browser-wasm.
///
/// FreeTypeSharp registers a DllImportResolver that throws PlatformNotSupportedException
/// on "browser", and Mono routes the assembly's P/Invokes through it. We replace that
/// resolver with one returning IntPtr.Zero, so resolution falls through to the wasm
/// static pinvoke table (where "freetype" is registered — see ZGF.Gui.Web.csproj).
/// SetDllImportResolver is set-once, so we force FreeTypeSharp's initializer to run,
/// then overwrite its entry in NativeLibrary's internal resolver table.
[SupportedOSPlatform("browser")]
internal static class WasmFreeTypeResolver
{
    private static IntPtr Resolve(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
        => IntPtr.Zero;

    [DynamicDependency("s_nativeDllResolveMap", typeof(NativeLibrary))]
    public static void Install()
    {
        RuntimeHelpers.RunClassConstructor(typeof(FT).TypeHandle);

        var ftAssembly = typeof(FT).Assembly;
        DllImportResolver ours = Resolve;

        var field = typeof(NativeLibrary).GetField("s_nativeDllResolveMap",
            BindingFlags.NonPublic | BindingFlags.Static);

        if (field?.GetValue(null) is ConditionalWeakTable<Assembly, DllImportResolver> map)
        {
            map.AddOrUpdate(ftAssembly, ours);
            return;
        }

        try { NativeLibrary.SetDllImportResolver(ftAssembly, ours); }
        catch (InvalidOperationException) { }
    }
}
