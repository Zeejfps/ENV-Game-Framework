namespace SlangIntegrationTest;

[Flags]
public enum SlangTargetFlags : uint
{
    /* When compiling for a D3D Shader Model 5.1 or higher target, allocate
          distinct register spaces for parameter blocks.

          @deprecated This behavior is now enabled unconditionally.
       */
    SLANG_TARGET_FLAG_PARAMETER_BLOCKS_USE_REGISTER_SPACES = 1 << 4,

    /* When set, will generate target code that contains all entrypoints defined
       in the input source or specified via the `spAddEntryPoint` function in a
       single output module (library/source file).
    */
    SLANG_TARGET_FLAG_GENERATE_WHOLE_PROGRAM = 1 << 8,

    /* When set, will dump out the IR between intermediate compilation steps.*/
    SLANG_TARGET_FLAG_DUMP_IR = 1 << 9,

    /* When set, will generate SPIRV directly rather than via glslang. */
    SLANG_TARGET_FLAG_GENERATE_SPIRV_DIRECTLY = 1 << 10,
}