using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SlangIntegrationTest;

[StructLayout(LayoutKind.Sequential)]
public struct TargetDesc
{
    public nuint StructureSize;

    public SlangCompileTarget Format;

    public SlangProfileID Profile;

    public SlangTargetFlags Flags;

    public SlangFloatingPointMode FloatingPointMode;

    public SlangLineDirectiveMode LineDirectiveMode;

    public byte ForceGLSLScalarBufferLayout;

    public IntPtr CompilerOptionEntries;

    public uint CompilerOptionEntryCount;

    public TargetDesc()
    {
        StructureSize = (nuint)Unsafe.SizeOf<TargetDesc>();
        Format = SlangCompileTarget.SLANG_TARGET_UNKNOWN;
        Profile = SlangProfileID.Unknown;
        Flags = SlangTargetFlags.SLANG_TARGET_FLAG_GENERATE_SPIRV_DIRECTLY;
        FloatingPointMode = SlangFloatingPointMode.SLANG_FLOATING_POINT_MODE_DEFAULT;
        LineDirectiveMode = SlangLineDirectiveMode.SLANG_LINE_DIRECTIVE_MODE_DEFAULT;
        ForceGLSLScalarBufferLayout = 0;
        CompilerOptionEntries = IntPtr.Zero;
        CompilerOptionEntryCount = 0;
    }
}
