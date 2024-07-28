namespace SlangIntegrationTest;

public enum SlangLineDirectiveMode : uint
{
    SLANG_LINE_DIRECTIVE_MODE_DEFAULT = 0,  /**< Default behavior: pick behavior base on target. */
    SLANG_LINE_DIRECTIVE_MODE_NONE,         /**< Don't emit line directives at all. */
    SLANG_LINE_DIRECTIVE_MODE_STANDARD,     /**< Emit standard C-style `#line` directives. */
    SLANG_LINE_DIRECTIVE_MODE_GLSL,         /**< Emit GLSL-style directives with file *number* instead of name */
    SLANG_LINE_DIRECTIVE_MODE_SOURCE_MAP,   /**< Use a source map to track line mappings (ie no #line will appear in emitting source) */
}