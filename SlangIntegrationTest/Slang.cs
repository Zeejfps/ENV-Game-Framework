using System.Runtime.InteropServices;
using System.Text;

namespace SlangIntegrationTest;

using SlangResult = Int32;
using SlangInt = Int64;

public static class SlangCompilerAPI
{
    // [ComImport]
    // [Guid("00000000-0000-0000-C000-000000000046")]
    // [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    // public interface ISlangUnknown
    // {
    //     // IUnknown methods
    //     [PreserveSig]
    //     int QueryInterface(ref Guid riid, out IntPtr ppvObject);
    //     
    //     [PreserveSig]
    //     uint AddRef();
    //     
    //     [PreserveSig]
    //     uint Release();
    // }
    //
    // [Guid("c140b5fd-0c78-452e-ba7c-1a1e70c7f71c")]
    // [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    // public interface IGlobalSession : ISlangUnknown
    // {
    //     SlangResult CreateSession(
    //         [In] in SessionDesc desc,
    //         [Out] out ISession session);
    // }
    
    // [ComImport]
    // [Guid("67618701-d116-468f-ab3b-474bedce0e3d")]
    // [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    // public interface ISession : ISlangUnknown
    // {
    //     [PreserveSig]
    //     IGlobalSession GetGlobalSession();
    //     
    //     IModule LoadModule(
    //         [In] IntPtr moduleName,
    //         [Out] out ISlangBlob? outDiagnostics);
    // }
    //
    // [ComImport]
    // [Guid("5bc42be8-5c50-4929-9e5e-d15e7c24015f")]
    // [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    // public interface IComponentType : ISlangUnknown
    // {
    //     
    // }
    //
    // [ComImport]
    // [Guid("0c720e64-8722-4d31-8990-638a98b1c279")]
    // [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    // public interface IModule : IComponentType
    // {
    //     
    // }
    //
    // [ComImport]
    // [Guid("8BA5FB08-5195-40e2-AC58-0D989C3A0102")]
    // [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    // public interface ISlangBlob : ISlangUnknown
    // {
    //     [PreserveSig]
    //     IntPtr GetBufferPointer();
    //     
    //     [PreserveSig]
    //     int GetBufferSize();
    // }
    //
    // public struct SessionDesc
    // {
    //     private int StructureSize;
    //
    //     public IntPtr Targets;
    //     public SlangInt TargetCount;
    //     
    //     public SessionFlags flags = SessionFlags.kSessionFlags_None;
    //
    //     public SlangMatrixLayoutMode DefaultMatrixLayoutMode = SlangMatrixLayoutMode.SLANG_MATRIX_LAYOUT_ROW_MAJOR;
    //
    //     public IntPtr SearchPaths;
    //     public SlangInt SearchPathCount;
    //     
    //     public IntPtr PreprocessorMacros;
    //     public SlangInt PreprocessorMacroCount;
    //     
    //     public IntPtr FileSystem;
    //     
    //     public bool EnableEffectAnnotations;
    //     public bool AllowGLSLSyntax;
    //
    //     public IntPtr CompilerOptionEntries;
    //     public UInt32 CompilerOptionEntryCount;
    //
    //     public SessionDesc()
    //     {
    //         StructureSize = Marshal.SizeOf<SessionDesc>();
    //     }
    // }
    //
    // public enum SlangMatrixLayoutMode : uint
    // {
    //     SLANG_MATRIX_LAYOUT_MODE_UNKNOWN = 0,
    //     SLANG_MATRIX_LAYOUT_ROW_MAJOR,
    //     SLANG_MATRIX_LAYOUT_COLUMN_MAJOR,
    // }
    //
    // public enum SessionFlags
    // {
    //     kSessionFlags_None = 0
    // }
    
    [DllImport("slang.dll")]
    public static extern SlangResult slang_createGlobalSession(SlangInt apiVersion, out IGlobalSession outGlobalSession);
}

[Guid("c140b5fd-0c78-452e-ba7c-1a1e70c7f71c")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IGlobalSession
{
    [PreserveSig]
    int CreateSession(
        [In] ref SessionDesc desc,
        [Out] out ISession outSession);

    [PreserveSig]
    int FindProfile(
        [MarshalAs(UnmanagedType.LPStr)] string name);

    void SetDownstreamCompilerPath(
        SlangPassThrough passThrough,
        [MarshalAs(UnmanagedType.LPStr)] string path);

    [Obsolete("Use SetLanguagePrelude instead")]
    void SetDownstreamCompilerPrelude(
        SlangPassThrough passThrough,
        [MarshalAs(UnmanagedType.LPStr)] string preludeText);

    [Obsolete("Use GetLanguagePrelude instead")]
    void GetDownstreamCompilerPrelude(
        SlangPassThrough passThrough,
        [Out] out ISlangBlob outPrelude);

    [return: MarshalAs(UnmanagedType.LPStr)]
    string GetBuildTagString();

    [PreserveSig]
    int SetDefaultDownstreamCompiler(
        SlangSourceLanguage sourceLanguage,
        SlangPassThrough defaultCompiler);

    SlangPassThrough GetDefaultDownstreamCompiler(
        SlangSourceLanguage sourceLanguage);

    void SetLanguagePrelude(
        SlangSourceLanguage sourceLanguage,
        [MarshalAs(UnmanagedType.LPStr)] string preludeText);

    void GetLanguagePrelude(
        SlangSourceLanguage sourceLanguage,
        [Out] out ISlangBlob outPrelude);

    [PreserveSig]
    int CreateCompileRequest(
        [Out] out ICompileRequest outCompileRequest);

    void AddBuiltins(
        [MarshalAs(UnmanagedType.LPStr)] string sourcePath,
        [MarshalAs(UnmanagedType.LPStr)] string sourceString);

    void SetSharedLibraryLoader(
        ISlangSharedLibraryLoader loader);

    ISlangSharedLibraryLoader GetSharedLibraryLoader();

    [PreserveSig]
    int CheckCompileTargetSupport(
        SlangCompileTarget target);

    [PreserveSig]
    int CheckPassThroughSupport(
        SlangPassThrough passThrough);

    [PreserveSig]
    int CompileStdLib(CompileStdLibFlags flags);

    [PreserveSig]
    int LoadStdLib(
        IntPtr stdLib,
        IntPtr stdLibSizeInBytes);

    [PreserveSig]
    int SaveStdLib(
        SlangArchiveType archiveType,
        [Out] out ISlangBlob outBlob);

    [PreserveSig]
    int FindCapability(
        [MarshalAs(UnmanagedType.LPStr)] string name);

    void SetDownstreamCompilerForTransition(
        SlangCompileTarget source,
        SlangCompileTarget target,
        SlangPassThrough compiler);

    SlangPassThrough GetDownstreamCompilerForTransition(
        SlangCompileTarget source,
        SlangCompileTarget target);

    void GetCompilerElapsedTime(
        out double outTotalTime,
        out double outDownstreamTime);

    [PreserveSig]
    int SetSPIRVCoreGrammar(
        [MarshalAs(UnmanagedType.LPStr)] string jsonPath);

    [PreserveSig]
    int ParseCommandLineArguments(
        int argc,
        [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPStr, SizeParamIndex = 0)] string[] argv,
        ref SessionDesc outSessionDesc,
        [Out] out ISlangUnknown outAuxAllocation);

    [PreserveSig]
    int GetSessionDescDigest(
        ref SessionDesc sessionDesc,
        [Out] out ISlangBlob outBlob);
}

// Note: These are placeholder declarations. You'll need to define these types separately.

public struct TargetDesc { }
public struct PreprocessorMacroDesc { }
public struct CompilerOptionEntry { }
public interface ISlangFileSystem { }
public interface ISlangBlob { }
public interface ICompileRequest { }
public interface ISlangSharedLibraryLoader { }
public interface ISlangUnknown { }
public interface IModule { }
public interface IBlob { }
public interface IComponentType { }
public class TypeReflection { }
public class TypeLayoutReflection { }
public struct SpecializationArg { }
public enum LayoutRules { Default }
public enum ContainerType { }
public class SlangCompileRequest { }
public interface ITypeConformance { }

public enum SlangPassThrough { }
public enum SlangSourceLanguage { }
public enum SlangCompileTarget { }
public enum CompileStdLibFlags { }
public enum SlangArchiveType { }
public enum SlangProfileID { }
public enum SlangCapabilityID { }