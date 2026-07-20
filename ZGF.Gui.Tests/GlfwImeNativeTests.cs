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

    [Fact]
    public void BundledGlfw_CanScopeTheImeToTextFields()
    {
        Assert.True(GlfwIme.IsTextInputFocusSupported,
            "The bundled GLFW native does not export glfwSetTextInputFocus, so the IME stays enabled " +
            "for the whole window and every bare-letter shortcut dies while a CJK input method is " +
            "active. LWJGL's builds never carried this — the natives must come from the glfw-natives " +
            "workflow. See Glfw.NET/Native/README.md.");
    }
}
