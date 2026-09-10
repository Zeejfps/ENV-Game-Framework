namespace ZGF.Desktop.Backends;

internal static class GlfwMonitors
{
    public static IReadOnlyList<MonitorWorkArea> WorkAreas()
    {
        var monitors = GLFW.Glfw.Monitors;
        var result = new MonitorWorkArea[monitors.Length];
        for (var i = 0; i < monitors.Length; i++)
        {
            var wa = monitors[i].WorkArea;
            var scaleX = monitors[i].ContentScale.X;
            result[i] = new MonitorWorkArea(wa.X, wa.Y, wa.Width, wa.Height, scaleX > 0f ? scaleX : 1f);
        }
        return result;
    }
}
