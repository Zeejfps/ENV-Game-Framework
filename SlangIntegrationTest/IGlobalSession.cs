using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SlangIntegrationTest;

[GeneratedComInterface(StringMarshalling = StringMarshalling.Utf8)]
[Guid("c140b5fd-0c78-452e-ba7c-1a1e70c7f71c")]
public partial interface IGlobalSession
{
    int CreateSession(
        ref SessionDesc desc,
        ref IntPtr outSession);

    int FindProfile(string name);

    void SetDownstreamCompilerPath(
        SlangPassThrough passThrough,
        string path);

    void SetDownstreamCompilerPrelude(
        SlangPassThrough passThrough,
        string preludeText);

    void GetDownstreamCompilerPrelude(
        SlangPassThrough passThrough,
        out IntPtr outPrelude);

    IntPtr GetBuildTagString();

    int SetDefaultDownstreamCompiler(
        SlangSourceLanguage sourceLanguage,
        SlangPassThrough defaultCompiler);

    SlangPassThrough GetDefaultDownstreamCompiler(
        SlangSourceLanguage sourceLanguage);

    void SetLanguagePrelude(
        SlangSourceLanguage sourceLanguage,
        string preludeText);

    void GetLanguagePrelude(
        SlangSourceLanguage sourceLanguage,
        out IntPtr outPrelude);

    int CreateCompileRequest(out IntPtr outCompileRequest);

    void AddBuiltins(
        string sourcePath,
        string sourceString);

    void SetSharedLibraryLoader(IntPtr loader);

    IntPtr GetSharedLibraryLoader();

    int CheckCompileTargetSupport(SlangCompileTarget target);

    int CheckPassThroughSupport(SlangPassThrough passThrough);

    int CompileStdLib(CompileStdLibFlags flags);

    int LoadStdLib(IntPtr stdLib, IntPtr stdLibSizeInBytes);

    int SaveStdLib(SlangArchiveType archiveType, out IntPtr outBlob);

    int FindCapability(string name);

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

    int SetSPIRVCoreGrammar(string jsonPath);

    int ParseCommandLineArguments(
        int argc,
        IntPtr argv,
        ref SessionDesc outSessionDesc,
        out IntPtr outAuxAllocation);

    int GetSessionDescDigest(
        ref SessionDesc sessionDesc,
        out IntPtr outBlob);
}
