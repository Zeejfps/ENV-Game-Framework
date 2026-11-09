using System.Runtime.InteropServices;
using static GL46;

namespace ZGF.Gui.Testing.Gpu;

/// <summary>What a context must provide for the OpenGL canvas and the offscreen target to run.</summary>
public static class GlRequirements
{
    public const int Major = 4;
    public const int Minor = 1;

    /// <summary>Every entry point the canvas (<c>ZGF.Gui.OpenGL</c>, with the shader helpers in
    /// <c>OpenGlUtils</c>) and the offscreen target call unconditionally.</summary>
    public static IReadOnlySet<string> Required { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        "glActiveTexture", "glAttachShader", "glBindBuffer", "glBindBufferBase", "glBindFramebuffer",
        "glBindTexture", "glBindVertexArray", "glBlendFuncSeparate", "glBufferData", "glBufferSubData",
        "glCheckFramebufferStatus", "glClear", "glClearColor", "glCompileShader", "glCreateProgram",
        "glCreateShader", "glDeleteBuffers", "glDeleteFramebuffers", "glDeleteProgram", "glDeleteShader",
        "glDeleteTextures", "glDeleteVertexArrays", "glDisable", "glDrawArraysInstanced", "glDrawBuffers",
        "glEnable", "glEnableVertexAttribArray", "glFramebufferTexture2D", "glGenBuffers",
        "glGenFramebuffers", "glGenTextures", "glGenVertexArrays", "glGetError", "glGetIntegerv",
        "glGetProgramInfoLog", "glGetProgramiv", "glGetShaderInfoLog", "glGetShaderiv", "glGetString",
        "glGetUniformBlockIndex", "glGetUniformLocation", "glLinkProgram", "glPixelStorei", "glReadBuffer",
        "glReadPixels", "glShaderSource", "glTexImage2D", "glTexParameteri", "glTexSubImage2D",
        "glUniform1i", "glUniform4f", "glUniformBlockBinding", "glUniformMatrix4fv", "glUseProgram",
        "glVertexAttribDivisor", "glVertexAttribIPointer", "glVertexAttribPointer", "glViewport",
    };

    /// <summary>Entry points the canvas calls only after checking <see cref="GL46.IsLoaded"/>.</summary>
    public static IReadOnlySet<string> Optional { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        "glDrawArraysInstancedBaseInstance",
    };

    /// <summary>Checks the context current on this thread, after <see cref="GL46.Import"/>, and describes it.</summary>
    /// <exception cref="NotSupportedException">The version is below 4.1 or a required entry point is missing.</exception>
    internal static unsafe string Verify()
    {
        var missing = Required.Where(name => !IsLoaded(name)).Order(StringComparer.Ordinal).ToList();
        if (missing.Contains("glGetIntegerv") || missing.Contains("glGetString"))
            throw new NotSupportedException($"The OpenGL context cannot report its version; missing: {string.Join(", ", missing)}.");

        int major = 0, minor = 0;
        glGetIntegerv(GL_MAJOR_VERSION, &major);
        glGetIntegerv(GL_MINOR_VERSION, &minor);
        var description = $"OpenGL {major}.{minor} ({String(GL_VERSION)}; {String(GL_RENDERER)})";

        if (major < Major || (major == Major && minor < Minor))
            throw new NotSupportedException($"{description}: OpenGL {Major}.{Minor} core is required.");
        if (missing.Count > 0)
            throw new NotSupportedException(
                $"{description} is missing entry points the renderer needs: {string.Join(", ", missing)}.");
        return description;
    }

    private static unsafe string String(uint name)
    {
        var value = glGetString(name);
        return value == null ? "unknown" : Marshal.PtrToStringAnsi((nint)value) ?? "unknown";
    }
}
