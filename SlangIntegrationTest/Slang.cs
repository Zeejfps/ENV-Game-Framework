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

[GeneratedComInterface(StringMarshalling = StringMarshalling.Utf8)]
[Guid("8f241361-f5bd-4ca0-a3ac-02f7fa2402b8")]
public partial interface IEntryPoint : IComponentType
{
    [PreserveSig] IntPtr GetFunctionReflection();
}

public enum SlangPassThrough
{
    None = 0,
    Fxc = 1,
    Dxc = 2,
    Glslang = 3,
    SpirvDis = 4,
    Clang = 5,
    VisualStudio = 6,
    Gcc = 7,
    GenericCCpp = 8,
    Nvrtc = 9,
    Llvm = 10,
    SpirvOpt = 11,
    Metal = 12,
    Tint = 13,
    SpirvLink = 14,
}

public enum SlangSourceLanguage
{
    Unknown = 0,
    Slang = 1,
    Hlsl = 2,
    Glsl = 3,
    C = 4,
    Cpp = 5,
    Cuda = 6,
    Spirv = 7,
    Metal = 8,
    Wgsl = 9,
    Llvm = 10,
}

public enum SlangStage
{
    None = 0,
    Vertex = 1,
    Hull = 2,
    Domain = 3,
    Geometry = 4,
    Fragment = 5,
    Compute = 6,
    RayGeneration = 7,
    Intersection = 8,
    AnyHit = 9,
    ClosestHit = 10,
    Miss = 11,
    Callable = 12,
    Mesh = 13,
    Amplification = 14,
    Dispatch = 15,
    Pixel = Fragment,
}

public enum CompileStdLibFlags { }
public enum SlangArchiveType { }
public enum SlangCapabilityID { }
