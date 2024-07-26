namespace OpenGlWrapper;

[Flags]
public enum FramebufferAccessFlag : uint
{
    Read = GL46.GL_READ_FRAMEBUFFER,
    Write = GL46.GL_DRAW_FRAMEBUFFER,
    ReadWrite = GL46.GL_FRAMEBUFFER
}