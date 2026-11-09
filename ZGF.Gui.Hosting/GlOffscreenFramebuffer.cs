using OpenGLSandbox;
using static GL46;

namespace ZGF.Gui.Hosting;

/// <summary>A framebuffer object with an RGBA8 colour attachment, in place of a window's own framebuffer.
/// Framebuffer objects are not shared between contexts, so it is created, drawn, read and deleted only
/// with its window's context current.</summary>
internal sealed unsafe class GlOffscreenFramebuffer : IDisposable
{
    private uint _framebuffer;
    private uint _color;

    public GlOffscreenFramebuffer(int pixelWidth, int pixelHeight)
    {
        uint framebuffer, color;
        glGenFramebuffers(1, &framebuffer);
        glGenTextures(1, &color);
        _framebuffer = framebuffer;
        _color = color;
        Allocate(pixelWidth, pixelHeight);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, (int)GL_NEAREST);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, (int)GL_NEAREST);
        glBindTexture(GL_TEXTURE_2D, 0);

        glBindFramebuffer(GL_FRAMEBUFFER, _framebuffer);
        glFramebufferTexture2D(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, GL_TEXTURE_2D, _color, 0);
        var attachment = GL_COLOR_ATTACHMENT0;
        glDrawBuffers(1, &attachment);
        glReadBuffer(GL_COLOR_ATTACHMENT0);
        ThrowOnGlError("creating an offscreen framebuffer");
        ThrowIfIncomplete();
        glBindFramebuffer(GL_FRAMEBUFFER, 0);
    }

    public int PixelWidth { get; private set; }
    public int PixelHeight { get; private set; }

    /// <summary>Makes this the draw and read framebuffer, <paramref name="pixelWidth"/> by
    /// <paramref name="pixelHeight"/>, with a viewport covering it and no scissor; reallocated, its contents
    /// undefined, when the size changed.</summary>
    public void Bind(int pixelWidth, int pixelHeight)
    {
        if (pixelWidth != PixelWidth || pixelHeight != PixelHeight)
            Allocate(pixelWidth, pixelHeight);
        glBindFramebuffer(GL_FRAMEBUFFER, _framebuffer);
        glViewport(0, 0, PixelWidth, PixelHeight);
        glDisable(GL_SCISSOR_TEST);
    }

    public static void Unbind() => glBindFramebuffer(GL_FRAMEBUFFER, 0);

    /// <summary>The pixels last drawn: RGBA, rows top-down, premultiplied alpha.</summary>
    public SurfaceImage Read()
    {
        var width = PixelWidth;
        var height = PixelHeight;
        var bottomUp = new byte[width * height * 4];
        glBindFramebuffer(GL_READ_FRAMEBUFFER, _framebuffer);
        glReadBuffer(GL_COLOR_ATTACHMENT0);
        glPixelStorei(GL_PACK_ALIGNMENT, 4);
        glPixelStorei(GL_PACK_ROW_LENGTH, 0);
        fixed (byte* pixels = bottomUp)
            glReadPixels(0, 0, width, height, GL_RGBA, GL_UNSIGNED_BYTE, pixels);
        glBindFramebuffer(GL_READ_FRAMEBUFFER, 0);
        ThrowOnGlError("reading an offscreen framebuffer");

        var rowBytes = width * 4;
        var topDown = new byte[bottomUp.Length];
        for (var y = 0; y < height; y++)
            Array.Copy(bottomUp, (height - 1 - y) * rowBytes, topDown, y * rowBytes, rowBytes);
        return new SurfaceImage(width, height, topDown);
    }

    private void Allocate(int pixelWidth, int pixelHeight)
    {
        glBindTexture(GL_TEXTURE_2D, _color);
        glTexImage2D(GL_TEXTURE_2D, 0, (int)GL_RGBA8, pixelWidth, pixelHeight, 0, GL_RGBA, GL_UNSIGNED_BYTE, null);
        ThrowOnGlError($"allocating a {pixelWidth}x{pixelHeight} RGBA8 colour attachment");
        PixelWidth = pixelWidth;
        PixelHeight = pixelHeight;
    }

    private static void ThrowIfIncomplete()
    {
        var status = glCheckFramebufferStatus(GL_FRAMEBUFFER);
        if (status != GL_FRAMEBUFFER_COMPLETE)
            throw new InvalidOperationException($"The offscreen framebuffer is incomplete (status 0x{status:X}).");
    }

    private static void ThrowOnGlError(string doing)
    {
        if (OpenGlUtils.TryGetGlError(out var error))
            throw new InvalidOperationException($"OpenGL reported {error} by the end of {doing}.");
    }

    public void Dispose()
    {
        var framebuffer = _framebuffer;
        var color = _color;
        if (framebuffer != 0) glDeleteFramebuffers(1, &framebuffer);
        if (color != 0) glDeleteTextures(1, &color);
        _framebuffer = 0;
        _color = 0;
    }
}
