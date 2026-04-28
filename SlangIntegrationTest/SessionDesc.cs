using System.Runtime.CompilerServices;
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

    public byte EnableEffectAnnotations;

    public byte AllowGLSLSyntax;

    public IntPtr CompilerOptionEntries;
    public uint CompilerOptionEntryCount;

    public SessionDesc()
    {
        StructureSize = Unsafe.SizeOf<SessionDesc>();
        Targets = IntPtr.Zero;
        TargetCount = 0;
        Flags = SessionFlags.None;
        DefaultMatrixLayoutMode = SlangMatrixLayoutMode.RowMajor;
        SearchPaths = IntPtr.Zero;
        SearchPathCount = 0;
        PreprocessorMacros = IntPtr.Zero;
        PreprocessorMacroCount = 0;
        FileSystem = IntPtr.Zero;
        EnableEffectAnnotations = 0;
        AllowGLSLSyntax = 0;
        CompilerOptionEntries = IntPtr.Zero;
        CompilerOptionEntryCount = 0;
    }

    public void SetSearchPaths(params string[] paths)
    {
        var pointerArray = new IntPtr[paths.Length];

        for (int i = 0; i < paths.Length; i++)
            pointerArray[i] = Marshal.StringToHGlobalAnsi(paths[i]);

        var result = Marshal.AllocHGlobal(IntPtr.Size * pointerArray.Length);
        Marshal.Copy(pointerArray, 0, result, pointerArray.Length);

        SearchPaths = result;
        SearchPathCount = paths.Length;
    }

    public unsafe void SetTargets(params TargetDesc[] targetDescriptions)
    {
        var structSize = Unsafe.SizeOf<TargetDesc>();
        var ptrToArrayOfStructs = Marshal.AllocHGlobal(structSize * targetDescriptions.Length);

        var basePtr = (TargetDesc*)ptrToArrayOfStructs;
        for (var i = 0; i < targetDescriptions.Length; i++)
            basePtr[i] = targetDescriptions[i];

        Targets = ptrToArrayOfStructs;
        TargetCount = targetDescriptions.Length;
    }
}
