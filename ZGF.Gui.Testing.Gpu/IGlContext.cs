namespace ZGF.Gui.Testing.Gpu;

/// <summary>A GL context current on the render thread that created it.</summary>
internal interface IGlContext : IDisposable
{
    /// <summary>The address of a GL entry point, or 0 when the driver has none.</summary>
    nint GetProcAddress(string name);
}
