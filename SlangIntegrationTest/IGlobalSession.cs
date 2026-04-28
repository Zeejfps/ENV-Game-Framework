using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SlangIntegrationTest;

[GeneratedComInterface(StringMarshalling = StringMarshalling.Utf8)]
[Guid("c140b5fd-0c78-452e-ba7c-1a1e70c7f71c")]
public partial interface IGlobalSession
{
    [PreserveSig] int CreateSession(ref SessionDesc desc, ref IntPtr outSession);

    [PreserveSig] int FindProfile(string name);

    [PreserveSig] void SetDownstreamCompilerPath(SlangPassThrough passThrough, string path);

    [PreserveSig] void SetDownstreamCompilerPrelude(SlangPassThrough passThrough, string preludeText);

    [PreserveSig] void GetDownstreamCompilerPrelude(SlangPassThrough passThrough, out IntPtr outPrelude);

    [PreserveSig] IntPtr GetBuildTagString();

    [PreserveSig] int SetDefaultDownstreamCompiler(SlangSourceLanguage sourceLanguage, SlangPassThrough defaultCompiler);

    [PreserveSig] SlangPassThrough GetDefaultDownstreamCompiler(SlangSourceLanguage sourceLanguage);

    [PreserveSig] void SetLanguagePrelude(SlangSourceLanguage sourceLanguage, string preludeText);

    [PreserveSig] void GetLanguagePrelude(SlangSourceLanguage sourceLanguage, out IntPtr outPrelude);

    [PreserveSig] int CreateCompileRequest(out IntPtr outCompileRequest);

    [PreserveSig] void AddBuiltins(string sourcePath, string sourceString);

    [PreserveSig] void SetSharedLibraryLoader(IntPtr loader);

    [PreserveSig] IntPtr GetSharedLibraryLoader();

    [PreserveSig] int CheckCompileTargetSupport(SlangCompileTarget target);

    [PreserveSig] int CheckPassThroughSupport(SlangPassThrough passThrough);

    [PreserveSig] int CompileStdLib(CompileStdLibFlags flags);

    [PreserveSig] int LoadStdLib(IntPtr stdLib, IntPtr stdLibSizeInBytes);

    [PreserveSig] int SaveStdLib(SlangArchiveType archiveType, out IntPtr outBlob);

    [PreserveSig] int FindCapability(string name);

    [PreserveSig] void SetDownstreamCompilerForTransition(SlangCompileTarget source, SlangCompileTarget target, SlangPassThrough compiler);

    [PreserveSig] SlangPassThrough GetDownstreamCompilerForTransition(SlangCompileTarget source, SlangCompileTarget target);

    [PreserveSig] void GetCompilerElapsedTime(out double outTotalTime, out double outDownstreamTime);

    [PreserveSig] int SetSPIRVCoreGrammar(string jsonPath);

    [PreserveSig] int ParseCommandLineArguments(int argc, IntPtr argv, ref SessionDesc outSessionDesc, out IntPtr outAuxAllocation);

    [PreserveSig] int GetSessionDescDigest(ref SessionDesc sessionDesc, out IntPtr outBlob);

    [PreserveSig] int CompileBuiltinModule(int module, CompileStdLibFlags flags);

    [PreserveSig] int LoadBuiltinModule(int module, IntPtr moduleData, IntPtr sizeInBytes);

    [PreserveSig] int SaveBuiltinModule(int module, SlangArchiveType archiveType, out IntPtr outBlob);
}
