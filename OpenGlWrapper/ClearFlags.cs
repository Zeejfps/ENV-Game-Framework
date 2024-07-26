using static GL46;

namespace OpenGlWrapper;

[Flags]
public enum ClearFlags : uint
{
    DepthBuffer = GL_DEPTH_BUFFER_BIT,
    StencilBuffer = GL_STENCIL_BUFFER_BIT,
    ColorBuffer = GL_COLOR_BUFFER_BIT,
    All = DepthBuffer | StencilBuffer | ColorBuffer,
}