using GLFW;
using static GL46;
using static OpenGLSandbox.OpenGlUtils;
using Monitor = GLFW.Monitor;

namespace ZGF.Desktop.Backends.OpenGl;

public sealed class OpenGlApp : GlfwApp<OpenGlWindow>
{
    public OpenGlApp(StartupConfig startupConfig)
        : base(startupConfig, ClientApi.OpenGL, CreateMainWindow)
    {
    }

    private static OpenGlWindow CreateMainWindow(Window window)
    {
        Glfw.MakeContextCurrent(window);
        Glfw.SwapInterval(1);
        Import(Glfw.GetProcAddress);
        GlDriverCheck.Verify();
        AssertNoGlError();
        return new OpenGlWindow(window, isMain: true);
    }

    protected override OpenGlWindow OpenWindow(int widthPoints, int heightPoints, string title, bool transparent)
    {
        if (transparent)
            Glfw.WindowHint(Hint.TransparentFramebuffer, true);

        // Share the GL context with the main window so the shared font atlas / textures
        // (GlSharedResources) are visible to this window's canvas.
        var glfw = Glfw.CreateWindow(widthPoints, heightPoints, title, Monitor.None, Main.GlfwWindow);

        Glfw.DefaultWindowHints();

        Glfw.MakeContextCurrent(glfw);
        // Popups and secondary windows must not gate vsync — each SwapBuffers on each context
        // serializes one vblank wait, so with vsync on N popups the loop becomes
        // refresh / (1 + N). Only the main window paces vsync.
        Glfw.SwapInterval(0);

        return new OpenGlWindow(glfw, isMain: false);
    }
}
