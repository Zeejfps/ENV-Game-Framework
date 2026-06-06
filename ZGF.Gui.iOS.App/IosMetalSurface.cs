using ZGF.Rendering.Metal;

namespace ZGF.Gui.iOS.App;

// The iOS implementation of the host-agnostic IMetalSurface seam (the desktop counterpart
// is ZGF.Desktop's MetalWindow). It just exposes the three native handles the shared
// MetalSurfaceRenderer drives each frame: the MTLDevice, a command queue, and the
// CAMetalLayer whose nextDrawable is rendered into and presented.
internal sealed class IosMetalSurface : IMetalSurface
{
    public IntPtr Device { get; }
    public IntPtr CommandQueue { get; }
    public IntPtr Layer { get; }

    public IosMetalSurface(IntPtr device, IntPtr commandQueue, IntPtr layer)
    {
        Device = device;
        CommandQueue = commandQueue;
        Layer = layer;
    }
}
