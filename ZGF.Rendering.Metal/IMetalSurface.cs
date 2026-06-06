namespace ZGF.Rendering.Metal;

/// <summary>
///     The host-provided Metal drawing surface that a <see cref="MetalSurfaceRenderer"/>
///     renders into each frame. It abstracts away <em>how</em> the surface is hosted
///     (a GLFW NSWindow on desktop, a UIView/CAMetalLayer on iOS) so the per-frame
///     render loop and the canvas renderer can be shared across platforms.
/// </summary>
public interface IMetalSurface
{
    /// <summary>The <c>MTLDevice</c> backing this surface.</summary>
    IntPtr Device { get; }

    /// <summary>A <c>MTLCommandQueue</c> created from <see cref="Device"/>.</summary>
    IntPtr CommandQueue { get; }

    /// <summary>
    ///     The <c>CAMetalLayer</c> whose <c>nextDrawable</c> is rendered into and presented
    ///     each frame.
    /// </summary>
    IntPtr Layer { get; }
}
