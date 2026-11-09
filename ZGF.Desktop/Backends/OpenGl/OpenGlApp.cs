using GLFW;
using static GL46;
using static OpenGLSandbox.OpenGlUtils;
using Monitor = GLFW.Monitor;

namespace ZGF.Desktop.Backends.OpenGl;

public sealed class OpenGlApp : GlfwApp<OpenGlWindow>
{
    private readonly GlEntryPointOwner _entryPoints;

    private OpenGlApp(StartupConfig startupConfig, GlEntryPointOwner entryPoints)
        : base(startupConfig, ClientApi.OpenGL, window => CreateMainWindow(window, startupConfig.Headless, entryPoints))
    {
        _entryPoints = entryPoints;
    }

    /// <summary>Opens the main window and its context, holding the process's GL entry points until disposed, after GLFW
    /// has destroyed every context.</summary>
    /// <exception cref="InvalidOperationException">Something else in the process, such as a GPU test device, holds them.</exception>
    public static OpenGlApp Create(StartupConfig startupConfig)
    {
        var entryPoints = GlEntryPointOwner.Claim("an OpenGL app window");
        try
        {
            return new OpenGlApp(startupConfig, entryPoints);
        }
        catch
        {
            Glfw.Terminate();
            entryPoints.Dispose();
            throw;
        }
    }

    protected override void OnTerminated() => _entryPoints.Dispose();

    private static OpenGlWindow CreateMainWindow(Window window, bool headless, GlEntryPointOwner entryPoints)
    {
        Glfw.MakeContextCurrent(window);
        Glfw.SwapInterval(1);
        Import(entryPoints, Glfw.GetProcAddress);
        GlDriverCheck.Verify();
        AssertNoGlError();
        return new OpenGlWindow(window, isMain: true, headless);
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

        return new OpenGlWindow(glfw, isMain: false, Headless);
    }
}
