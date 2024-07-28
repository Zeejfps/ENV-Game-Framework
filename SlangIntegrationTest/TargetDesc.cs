using System.Runtime.InteropServices;

namespace SlangIntegrationTest;

[StructLayout(LayoutKind.Sequential)]
public struct TargetDesc
{
    public IntPtr StructureSize;

    public SlangCompileTarget Format;

    public SlangProfileID Profile;

    public SlangTargetFlags Flags;

    public SlangFloatingPointMode FloatingPointMode;

    public SlangLineDirectiveMode LineDirectiveMode;

    [MarshalAs(UnmanagedType.U1)]
    public bool ForceGLSLScalarBufferLayout;

    public IntPtr CompilerOptionEntries;

    public uint CompilerOptionEntryCount;

    public TargetDesc()
    {
        StructureSize = Marshal.SizeOf<TargetDesc>();
        Format = SlangCompileTarget.Unknown;
        Profile = SlangProfileID.Unknown;
        Flags = SlangTargetFlags.Default;
        FloatingPointMode = SlangFloatingPointMode.Default;
        LineDirectiveMode = SlangLineDirectiveMode.Default;
        ForceGLSLScalarBufferLayout = false;
        CompilerOptionEntries = IntPtr.Zero;
        CompilerOptionEntryCount = 0;
    }
}