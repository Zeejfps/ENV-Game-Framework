using System.Runtime.InteropServices;

namespace SlangIntegrationTest;

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