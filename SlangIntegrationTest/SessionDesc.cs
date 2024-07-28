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
        DefaultMatrixLayoutMode = SlangMatrixLayoutMode.Unknown;
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

    public void SetSearchPaths(params string[] paths)
    {
        // Allocate an array of pointers
        var pointerArray = new IntPtr[paths.Length];
        
        // Allocate and marshal each string
        for (int i = 0; i < paths.Length; i++)
            pointerArray[i] = Marshal.StringToHGlobalAnsi(paths[i]);
        
        // Allocate memory for the array of pointers
        var result = Marshal.AllocHGlobal(IntPtr.Size * pointerArray.Length);
        Marshal.Copy(pointerArray, 0, result, pointerArray.Length);

        SearchPaths = result;
        SearchPathCount = paths.Length;
    }

    public void SetTargets(params TargetDesc[] targetDescriptions)
    {
        var pointerArray = new IntPtr[targetDescriptions.Length];
        for (int i = 0; i < targetDescriptions.Length; i++)
        {
            var targetDesc = targetDescriptions[i];
            var ptr = Marshal.AllocHGlobal(targetDesc.StructureSize);
            Marshal.StructureToPtr(targetDesc, ptr, false);
            pointerArray[i] = ptr;
        }
    }
}