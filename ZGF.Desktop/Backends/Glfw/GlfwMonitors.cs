namespace ZGF.Desktop.Backends.Glfw;

internal static class GlfwMonitors
{
    public static IReadOnlyList<MonitorWorkArea> WorkAreas()
    {
        var monitors = GLFW.Glfw.Monitors;
        var result = new MonitorWorkArea[monitors.Length];
        for (var i = 0; i < monitors.Length; i++)
        {
            var wa = monitors[i].WorkArea;
            result[i] = new MonitorWorkArea(wa.X, wa.Y, wa.Width, wa.Height);
        }
        return result;
    }
}
