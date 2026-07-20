using System.Runtime.InteropServices;
using GLFW;

namespace ZGF.Desktop.Backends.OpenGl;

public sealed class OpenGlWindow : GlfwWindowBase
{
    public OpenGlWindow(Window window, bool isMain) : base(window, isMain)
    {
        NativeHandle = ComputeNativeHandle(window);
        DpiScaleValue = ComputeDpiScale();
    }

    public override IntPtr NativeHandle { get; }

    public override void MakeContextCurrent() => Glfw.MakeContextCurrent(GlfwWindow);

    protected override void Present() => Glfw.SwapBuffers(GlfwWindow);

    protected override float ComputeDpiScale()
    {
        Glfw.GetFramebufferSize(GlfwWindow, out var fbW, out var fbH);
        Glfw.GetWindowSize(GlfwWindow, out var winW, out var winH);
        if (winW > 0 && winH > 0)
            return MathF.Max((float)fbW / winW, (float)fbH / winH);
        return 1f;
    }

    private static IntPtr ComputeNativeHandle(Window window)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return Native.GetWin32Window(window);
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return Native.GetX11Window(window);
        return window;
    }
}
