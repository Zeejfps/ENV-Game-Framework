using GLFW;

namespace ZGF.Gui.Tests;

/// <summary>
/// The IME depends on a GLFW native carrying the IM-support patch, which stock GLFW does not have.
/// Swap in a stock build — an upstream release archive, a distro's libglfw3 — and nothing breaks
/// loudly: the app still builds and runs, CJK input just stops working. This is the guard against
/// that, and against a future GLFW bump that drops the patch.
/// </summary>
public class GlfwImeNativeTests
{
    [Fact]
    public void BundledGlfw_CarriesTheImePatch()
    {
        Assert.True(GlfwIme.IsSupported,
            "The bundled GLFW native does not export glfwSetPreeditCallback, so it is not the " +
            "patched build. CJK text input will silently do nothing. See Glfw.NET/Native/README.md.");
    }
}
