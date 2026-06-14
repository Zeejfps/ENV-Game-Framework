using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;

namespace ZGF.Gui.Web.Rendering;

/// <summary>
/// Thin [JSImport] binding over the WebGL2 shim in <c>webgl2.js</c>. All GL
/// objects (buffers, VAOs, programs, shaders, textures, uniform locations) are
/// represented as <c>int</c> handles managed by the JS side, so the C# canvas can
/// mirror the desktop GL backend's integer-handle structure. Call
/// <see cref="InitAsync"/> once before any other method.
///
/// Only the WebGL2 entrypoints the canvas backend actually uses are bound — not a
/// full GL surface. See docs/web-font-rendering.md.
/// </summary>
[SupportedOSPlatform("browser")]
internal static partial class Webgl2
{
    public static async Task<bool> InitAsync(string canvasSelector)
    {
        // Path is relative to the runtime module in _framework/, so step up to the
        // bundle root where the shim is deployed (WasmExtraFilesToDeploy).
        await JSHost.ImportAsync("webgl2", "../webgl2.js");
        return Init(canvasSelector) != 0;
    }

    [JSImport("init", "webgl2")] private static partial int Init(string canvasSelector);

    // ---- per-frame state ----
    [JSImport("viewport", "webgl2")] public static partial void Viewport(int x, int y, int w, int h);
    [JSImport("clearColor", "webgl2")] public static partial void ClearColor(float r, float g, float b, float a);
    [JSImport("clear", "webgl2")] public static partial void Clear(int mask);
    [JSImport("enable", "webgl2")] public static partial void Enable(int cap);
    [JSImport("disable", "webgl2")] public static partial void Disable(int cap);
    [JSImport("blendFunc", "webgl2")] public static partial void BlendFunc(int src, int dst);
    [JSImport("activeTexture", "webgl2")] public static partial void ActiveTexture(int unit);

    // ---- shaders / programs ----
    [JSImport("createShader", "webgl2")] public static partial int CreateShader(int type);
    [JSImport("shaderSource", "webgl2")] public static partial void ShaderSource(int shader, string src);
    [JSImport("compileShader", "webgl2")] public static partial void CompileShader(int shader);
    [JSImport("getShaderCompiled", "webgl2")] public static partial int GetShaderCompiled(int shader);
    [JSImport("getShaderInfoLog", "webgl2")] public static partial string GetShaderInfoLog(int shader);
    [JSImport("createProgram", "webgl2")] public static partial int CreateProgram();
    [JSImport("attachShader", "webgl2")] public static partial void AttachShader(int program, int shader);
    [JSImport("linkProgram", "webgl2")] public static partial void LinkProgram(int program);
    [JSImport("getProgramLinked", "webgl2")] public static partial int GetProgramLinked(int program);
    [JSImport("getProgramInfoLog", "webgl2")] public static partial string GetProgramInfoLog(int program);
    [JSImport("useProgram", "webgl2")] public static partial void UseProgram(int program);
    [JSImport("getUniformLocation", "webgl2")] public static partial int GetUniformLocation(int program, string name);
    [JSImport("uniform1i", "webgl2")] public static partial void Uniform1i(int loc, int v);

    [JSImport("uniformMatrix4fv", "webgl2")]
    public static partial void UniformMatrix4fv(int loc, [JSMarshalAs<JSType.MemoryView>] Span<byte> mat16);

    [JSImport("getUniformBlockIndex", "webgl2")] public static partial int GetUniformBlockIndex(int program, string name);
    [JSImport("uniformBlockBinding", "webgl2")] public static partial void UniformBlockBinding(int program, int blockIndex, int binding);

    // ---- buffers ----
    [JSImport("createBuffer", "webgl2")] public static partial int CreateBuffer();
    [JSImport("bindBuffer", "webgl2")] public static partial void BindBuffer(int target, int buffer);
    [JSImport("bufferDataSize", "webgl2")] public static partial void BufferDataSize(int target, int size, int usage);

    [JSImport("bufferSubData", "webgl2")]
    public static partial void BufferSubData(int target, int offset, [JSMarshalAs<JSType.MemoryView>] Span<byte> data);

    [JSImport("bindBufferBase", "webgl2")] public static partial void BindBufferBase(int target, int index, int buffer);

    // ---- vertex arrays ----
    [JSImport("createVertexArray", "webgl2")] public static partial int CreateVertexArray();
    [JSImport("bindVertexArray", "webgl2")] public static partial void BindVertexArray(int vao);
    [JSImport("enableVertexAttribArray", "webgl2")] public static partial void EnableVertexAttribArray(int index);
    [JSImport("vertexAttribPointer", "webgl2")] public static partial void VertexAttribPointer(int index, int size, int type, int normalized, int stride, int offset);
    [JSImport("vertexAttribIPointer", "webgl2")] public static partial void VertexAttribIPointer(int index, int size, int type, int stride, int offset);
    [JSImport("vertexAttribDivisor", "webgl2")] public static partial void VertexAttribDivisor(int index, int divisor);

    // ---- textures ----
    [JSImport("createTexture", "webgl2")] public static partial int CreateTexture();
    [JSImport("bindTexture", "webgl2")] public static partial void BindTexture(int target, int texture);
    [JSImport("texImage2DSize", "webgl2")] public static partial void TexImage2DSize(int target, int level, int internalFormat, int w, int h, int border, int format, int type);

    [JSImport("texImage2DData", "webgl2")]
    public static partial void TexImage2DData(int target, int level, int internalFormat, int w, int h, int border, int format, int type, [JSMarshalAs<JSType.MemoryView>] Span<byte> data);

    [JSImport("texSubImage2D", "webgl2")]
    public static partial void TexSubImage2D(int target, int level, int x, int y, int w, int h, int format, int type, [JSMarshalAs<JSType.MemoryView>] Span<byte> data);

    [JSImport("texParameteri", "webgl2")] public static partial void TexParameteri(int target, int pname, int param);
    [JSImport("pixelStorei", "webgl2")] public static partial void PixelStorei(int pname, int param);

    // ---- draw ----
    [JSImport("drawArraysInstanced", "webgl2")] public static partial void DrawArraysInstanced(int mode, int first, int count, int instanceCount);
}

/// WebGL2 numeric constants (identical to the desktop GL enum values) used by the
/// canvas backend. Subset only.
internal static class Gl
{
    public const int TRIANGLES = 0x0004;
    public const int FLOAT = 0x1406;
    public const int UNSIGNED_INT = 0x1405;
    public const int UNSIGNED_BYTE = 0x1401;
    public const int ARRAY_BUFFER = 0x8892;
    public const int UNIFORM_BUFFER = 0x8A11;
    public const int DYNAMIC_DRAW = 0x88E8;
    public const int STATIC_DRAW = 0x88E4;
    public const int VERTEX_SHADER = 0x8B31;
    public const int FRAGMENT_SHADER = 0x8B30;
    public const int TEXTURE_2D = 0x0DE1;
    public const int TEXTURE0 = 0x84C0;
    public const int RED = 0x1903;
    public const int R8 = 0x8229;
    public const int RGBA = 0x1908;
    public const int TEXTURE_MIN_FILTER = 0x2801;
    public const int TEXTURE_MAG_FILTER = 0x2800;
    public const int TEXTURE_WRAP_S = 0x2802;
    public const int TEXTURE_WRAP_T = 0x2803;
    public const int LINEAR = 0x2601;
    public const int CLAMP_TO_EDGE = 0x812F;
    public const int UNPACK_ALIGNMENT = 0x0CF5;
    public const int BLEND = 0x0BE2;
    public const int DEPTH_TEST = 0x0B71;
    public const int SRC_ALPHA = 0x0302;
    public const int ONE_MINUS_SRC_ALPHA = 0x0303;
    public const int COLOR_BUFFER_BIT = 0x4000;
}
