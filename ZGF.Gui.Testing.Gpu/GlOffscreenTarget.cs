using OpenGLSandbox;
using static GL46;

namespace ZGF.Gui.Testing.Gpu;

/// <summary>A framebuffer object with an RGBA8 colour attachment, the size of <see cref="Size"/> in
/// physical pixels. Its members marshal to the device's render thread, and the device disposes it there
/// if it is still open when the device shuts down.</summary>
public sealed unsafe class GlOffscreenTarget : IDisposable
{
    private readonly GpuDevice _device;
    private uint _framebuffer;
    private uint _color;
    private bool _released;

    private GlOffscreenTarget(GpuDevice device, TargetSize size)
    {
        _device = device;
        Size = size;
    }

    public TargetSize Size { get; private set; }

    /// <exception cref="ArgumentOutOfRangeException">The size exceeds what the context can render to.</exception>
    public static GlOffscreenTarget Create(GpuDevice device, TargetSize size) => device.Run(() =>
    {
        ThrowIfBeyondLimits(size);
        var target = new GlOffscreenTarget(device, size);
        device.Own(target);
        try
        {
            target.Allocate();
            return target;
        }
        catch
        {
            target.Dispose();
            throw;
        }
    });

    /// <summary>Changes the size; the contents are undefined until the next draw.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The size exceeds what the context can render to.</exception>
    public void Resize(TargetSize size) => _device.Run(() =>
    {
        ObjectDisposedException.ThrowIf(_released, this);
        ThrowIfBeyondLimits(size);
        var reallocate = size.PixelWidth != Size.PixelWidth || size.PixelHeight != Size.PixelHeight;
        Size = size;
        if (!reallocate) return;
        AllocateColorStorage();
        glBindFramebuffer(GL_FRAMEBUFFER, _framebuffer);
        ThrowIfIncomplete();
    });

    /// <summary>Makes this the draw and read framebuffer with a viewport covering it, and no scissor or
    /// sRGB conversion in effect.</summary>
    public void Bind() => _device.Run(() =>
    {
        ObjectDisposedException.ThrowIf(_released, this);
        glBindFramebuffer(GL_DRAW_FRAMEBUFFER, _framebuffer);
        glBindFramebuffer(GL_READ_FRAMEBUFFER, _framebuffer);
        glViewport(0, 0, Size.PixelWidth, Size.PixelHeight);
        glDisable(GL_SCISSOR_TEST);
        glDisable(GL_FRAMEBUFFER_SRGB);
    });

    /// <summary>Fills the target with <paramref name="argb"/>, a straight-alpha packed colour, stored premultiplied.</summary>
    public void Clear(uint argb) => _device.Run(() =>
    {
        Bind();
        var a = (argb >> 24) / 255f;
        glClearColor((byte)(argb >> 16) / 255f * a, (byte)(argb >> 8) / 255f * a, (byte)argb / 255f * a, a);
        glClear(GL_COLOR_BUFFER_BIT);
        ThrowOnGlError("clearing the offscreen target");
    });

    /// <summary>The target's pixels: RGBA, rows top-down, premultiplied alpha.</summary>
    public RgbaImage Read() => _device.Run(() =>
    {
        ObjectDisposedException.ThrowIf(_released, this);
        var width = Size.PixelWidth;
        var height = Size.PixelHeight;
        var bottomUp = new byte[Size.ByteCount];
        glBindFramebuffer(GL_READ_FRAMEBUFFER, _framebuffer);
        glReadBuffer(GL_COLOR_ATTACHMENT0);
        glPixelStorei(GL_PACK_ALIGNMENT, 4);
        glPixelStorei(GL_PACK_ROW_LENGTH, 0);
        fixed (byte* pixels = bottomUp)
            glReadPixels(0, 0, width, height, GL_RGBA, GL_UNSIGNED_BYTE, pixels);
        ThrowOnGlError("reading the offscreen target");

        var rowBytes = width * 4;
        var topDown = new byte[bottomUp.Length];
        for (var y = 0; y < height; y++)
            Array.Copy(bottomUp, (height - 1 - y) * rowBytes, topDown, y * rowBytes, rowBytes);
        return new RgbaImage(width, height, topDown);
    });

    private static void ThrowIfBeyondLimits(TargetSize size)
    {
        int maxTexture, maxRenderbuffer;
        var maxViewport = stackalloc int[2];
        glGetIntegerv(GL_MAX_TEXTURE_SIZE, &maxTexture);
        glGetIntegerv(GL_MAX_RENDERBUFFER_SIZE, &maxRenderbuffer);
        glGetIntegerv(GL_MAX_VIEWPORT_DIMS, maxViewport);
        var maxWidth = Math.Min(Math.Min(maxTexture, maxRenderbuffer), maxViewport[0]);
        var maxHeight = Math.Min(Math.Min(maxTexture, maxRenderbuffer), maxViewport[1]);
        if (size.PixelWidth > maxWidth || size.PixelHeight > maxHeight)
            throw new ArgumentOutOfRangeException(nameof(size),
                $"{size.PixelWidth}x{size.PixelHeight} pixels exceeds the context's {maxWidth}x{maxHeight} limit " +
                $"(texture {maxTexture}, renderbuffer {maxRenderbuffer}, viewport {maxViewport[0]}x{maxViewport[1]}).");
    }

    private void Allocate()
    {
        uint framebuffer, color;
        glGenFramebuffers(1, &framebuffer);
        _framebuffer = framebuffer;
        glGenTextures(1, &color);
        _color = color;

        AllocateColorStorage();
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, (int)GL_NEAREST);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, (int)GL_NEAREST);
        glBindTexture(GL_TEXTURE_2D, 0);

        glBindFramebuffer(GL_FRAMEBUFFER, _framebuffer);
        glFramebufferTexture2D(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, GL_TEXTURE_2D, _color, 0);
        var attachment = GL_COLOR_ATTACHMENT0;
        glDrawBuffers(1, &attachment);
        glReadBuffer(GL_COLOR_ATTACHMENT0);
        ThrowOnGlError("creating the offscreen target");
        ThrowIfIncomplete();
    }

    private void AllocateColorStorage()
    {
        glBindTexture(GL_TEXTURE_2D, _color);
        glTexImage2D(GL_TEXTURE_2D, 0, (int)GL_RGBA8, Size.PixelWidth, Size.PixelHeight, 0, GL_RGBA, GL_UNSIGNED_BYTE, null);
        ThrowOnGlError($"allocating a {Size.PixelWidth}x{Size.PixelHeight} RGBA8 colour attachment");
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

    /// <summary>Deletes the framebuffer on the render thread. Once the device has stopped accepting work,
    /// its shutdown releases the target instead.</summary>
    public void Dispose() => _device.TryRun(Release);

    private void Release()
    {
        if (_released) return;
        _released = true;
        _device.Disown(this);
        var framebuffer = _framebuffer;
        var color = _color;
        if (framebuffer != 0) glDeleteFramebuffers(1, &framebuffer);
        if (color != 0) glDeleteTextures(1, &color);
        _framebuffer = 0;
        _color = 0;
    }
}
