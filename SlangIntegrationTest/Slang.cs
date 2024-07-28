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
    public static extern SlangResult slang_createGlobalSession(SlangInt apiVersion, ref IntPtr outGlobalSessionPtr);
    
    [DllImport("slang.dll", EntryPoint = "shutdown", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true, PreserveSig = true)]
    public static extern void slang_shutdown();

}

// Note: These are placeholder declarations. You'll need to define these types separately.

// Enums used in TargetDesc

public struct PreprocessorMacroDesc { }
public struct CompilerOptionEntry { }
public interface ISlangFileSystem { }
public interface ICompileRequest { }
public interface ISlangSharedLibraryLoader { }

public class TypeReflection { }
public class TypeLayoutReflection { }
public struct SpecializationArg { }
public enum LayoutRules { Default }
public enum ContainerType { }
public class SlangCompileRequest { }
public interface ITypeConformance { }

[Guid("8f241361-f5bd-4ca0-a3ac-02f7fa2402b8")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IEntryPoint
{
    
}

public enum SlangPassThrough { }
public enum SlangSourceLanguage { }
public enum CompileStdLibFlags { }
public enum SlangArchiveType { }
public enum SlangCapabilityID { }