using System.Runtime.InteropServices;

namespace SlangIntegrationTest;

[StructLayout(LayoutKind.Sequential)]
public struct SessionDesc
{
    public IntPtr StructureSize;

    public IntPtr Targets;
    public int TargetCount;

    public SessionFlags Flags;

    public SlangMatrixLayoutMode DefaultMatrixLayoutMode;

    public IntPtr SearchPaths;
    public int SearchPathCount;

    public IntPtr PreprocessorMacros;
    public int PreprocessorMacroCount;

    public IntPtr FileSystem;

    [MarshalAs(UnmanagedType.U1)]
    public bool EnableEffectAnnotations;

    [MarshalAs(UnmanagedType.U1)]
    public bool AllowGLSLSyntax;

    public IntPtr CompilerOptionEntries;
    public uint CompilerOptionEntryCount;

    public SessionDesc()
    {
        StructureSize = Marshal.SizeOf<SessionDesc>();
        Targets = IntPtr.Zero;
        TargetCount = 0;
        Flags = SessionFlags.None;
        DefaultMatrixLayoutMode = SlangMatrixLayoutMode.RowMajor;
        SearchPaths = IntPtr.Zero;
        SearchPathCount = 0;
        PreprocessorMacros = IntPtr.Zero;
        PreprocessorMacroCount = 0;
        FileSystem = IntPtr.Zero;
        EnableEffectAnnotations = false;
        AllowGLSLSyntax = false;
        CompilerOptionEntries = IntPtr.Zero;
        CompilerOptionEntryCount = 0;
    }
}