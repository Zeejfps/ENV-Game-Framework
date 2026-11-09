using ZGF.Desktop;
using ZGF.Gui.Desktop;
using ZGF.Gui.Testing.Gpu;

namespace ZGF.Gui.Tests;

/// <summary>A test that opens a real GLFW window and an OpenGL <see cref="GpuDevice"/>, skipped where this test thread
/// cannot open the one or the platform has no OpenGL backend for the other.</summary>
public sealed class GlfwAndOpenGlFactAttribute : FactAttribute
{
    public GlfwAndOpenGlFactAttribute()
    {
        if (OperatingSystem.IsMacOS())
            Skip = "GLFW runs only on the process's main thread on macOS, which a test does not have.";
        else if (OperatingSystem.IsLinux() && Environment.GetEnvironmentVariable("DISPLAY") == null
                 && Environment.GetEnvironmentVariable("WAYLAND_DISPLAY") == null)
            Skip = "GLFW needs a display server on Linux.";
        else if (GpuDevice.PlatformBackend is not (GpuBackend.Wgl or GpuBackend.Egl))
            Skip = "The platform has no OpenGL GpuDevice.";
    }
}

[Collection(GpuLifecycleCollection.Name)]
public sealed class FailedHostCreationTests
{
    [GlfwAndOpenGlFact]
    public void A_host_whose_construction_fails_after_its_window_opened_lets_go_of_GLFW_and_the_GL_entry_points()
    {
        var builder = GuiApp.CreateBuilder(new StartupConfig { WindowWidth = 320, WindowHeight = 240, WindowTitle = "Failing host", StartHidden = true })
            .UseRenderBackend(GuiRenderBackendKind.OpenGl)
            .UseStartup(_ => throw new InvalidOperationException("startup failed"));

        var failed = Assert.Throws<InvalidOperationException>(() => builder.Build(_ => throw new InvalidOperationException("never mounted")));
        Assert.Equal("startup failed", failed.Message);

        using var device = GpuDevice.Create();
        Assert.True(device.Run(() => device.IsRenderThread));
    }
}
