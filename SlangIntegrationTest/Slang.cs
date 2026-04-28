using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using SlangResult = System.Int32;
using SlangInt = System.Int64;

[assembly: DisableRuntimeMarshalling]

namespace SlangIntegrationTest;

public static partial class SlangCompilerAPI
{
    public const string LibraryName = "slang";

    public static StrategyBasedComWrappers ComWrappers { get; } = new();

    [ModuleInitializer]
    internal static void RegisterResolver()
    {
        NativeLibrary.SetDllImportResolver(typeof(SlangCompilerAPI).Assembly, Resolve);
    }

    private static IntPtr Resolve(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (libraryName != LibraryName)
            return IntPtr.Zero;

        // On macOS, slang_createGlobalSession lives in libslang-compiler.dylib,
        // not libslang.dylib (which is only a small runtime helper).
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return NativeLibrary.Load("slang-compiler", assembly, searchPath);

        return IntPtr.Zero;
    }

    [LibraryImport(LibraryName)]
    public static partial SlangResult slang_createGlobalSession(SlangInt apiVersion, ref IntPtr outGlobalSessionPtr);

    [LibraryImport(LibraryName, EntryPoint = "shutdown")]
    public static partial void slang_shutdown();
}

public struct PreprocessorMacroDesc { }
public struct CompilerOptionEntry { }
public struct SpecializationArg { }
public enum LayoutRules { Default }
public enum ContainerType { }

[GeneratedComInterface]
[Guid("8f241361-f5bd-4ca0-a3ac-02f7fa2402b8")]
public partial interface IEntryPoint
{
}

public enum SlangPassThrough { }
public enum SlangSourceLanguage { }
public enum CompileStdLibFlags { }
public enum SlangArchiveType { }
public enum SlangCapabilityID { }
