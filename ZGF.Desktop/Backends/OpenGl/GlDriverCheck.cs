using System.Runtime.InteropServices;
using static GL46;

namespace ZGF.Desktop.Backends.OpenGl;

/// <summary>Decides whether the GL context the driver gave us can run the renderer.</summary>
internal static class GlDriverCheck
{
    // The canvas shaders are #version 410, so 4.1 is the hard floor. The binding covers all of GL 4.6,
    // and drivers stopping short of that leave entry points unresolved — normal, and not fatal, because
    // everything past 4.1 is either unused or gated on GL46.IsLoaded at its call site.
    private const int RequiredMajor = 4;
    private const int RequiredMinor = 1;

    public static void Verify()
    {
        var (major, minor) = ContextVersion();
        if (major < RequiredMajor || (major == RequiredMajor && minor < RequiredMinor))
            throw new NotSupportedException(
                $"This system's graphics driver reports OpenGL {major}.{minor}, but OpenGL " +
                $"{RequiredMajor}.{RequiredMinor} is required. Updating the graphics driver usually fixes this. " +
                $"(renderer: {Describe(GL_RENDERER)}, version: {Describe(GL_VERSION)})");

        var missing = MissingFunctions;
        if (missing.Count == 0) return;

        Console.Error.WriteLine(
            $"OpenGL: {Describe(GL_VERSION)} did not provide {missing.Count} of the GL 4.6 entry points. " +
            $"Rendering only uses {RequiredMajor}.{RequiredMinor}, so this is expected on older drivers: " +
            string.Join(", ", missing.Take(12)) + (missing.Count > 12 ? ", ..." : ""));
    }

    private static (int Major, int Minor) ContextVersion()
    {
        // glGetIntegerv(GL_MAJOR_VERSION) is itself GL 3.0+ and raises GL_INVALID_ENUM below that;
        // the version string has been there since 1.0 and is what a too-old driver can still answer.
        var version = Describe(GL_VERSION);
        var digits = version.AsSpan();
        var dot = digits.IndexOf('.');
        if (dot <= 0) return (0, 0);

        var majorStart = dot;
        while (majorStart > 0 && char.IsAsciiDigit(digits[majorStart - 1])) majorStart--;

        var minorEnd = dot + 1;
        while (minorEnd < digits.Length && char.IsAsciiDigit(digits[minorEnd])) minorEnd++;

        return int.TryParse(digits[majorStart..dot], out var major)
               && int.TryParse(digits[(dot + 1)..minorEnd], out var minor)
            ? (major, minor)
            : (0, 0);
    }

    private static unsafe string Describe(uint name)
    {
        if (!IsLoaded("glGetString")) return "unknown";
        var value = glGetString(name);
        return value == null ? "unknown" : Marshal.PtrToStringAnsi((IntPtr)value) ?? "unknown";
    }
}
