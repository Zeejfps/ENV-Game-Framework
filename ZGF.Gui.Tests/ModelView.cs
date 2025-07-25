using static GL46;
using static OpenGLSandbox.OpenGlUtils;

namespace ZGF.Gui.Tests;

public sealed unsafe class ModelView : View
{
    private uint _frameBufferId;

    protected override void OnAttachedToContext(Context context)
    {
        base.OnAttachedToContext(context);
        uint frameBufferId;

        glGenFramebuffers(1, &frameBufferId);
        AssertNoGlError();

        _frameBufferId = frameBufferId;

        glBindFramebuffer(GL_FRAMEBUFFER, _frameBufferId);
        AssertNoGlError();

        uint colorTextureId;
        glGenTextures(1, &colorTextureId);
        glBindTexture(GL_TEXTURE_2D, colorTextureId);
        glTexImage2D(GL_TEXTURE_2D, 0, (int)GL_RGBA8, 640, 480, 0, GL_RGBA, GL_UNSIGNED_BYTE, (void*)0);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, (int)GL_LINEAR);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, (int)GL_LINEAR);
        glFramebufferTexture2D(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, GL_TEXTURE_2D, colorTextureId, 0);
        AssertNoGlError();

        var attachment = GL_COLOR_ATTACHMENT0;
        glDrawBuffers(1, &attachment);
        AssertNoGlError();

        if (glCheckFramebufferStatus(GL_FRAMEBUFFER) != GL_FRAMEBUFFER_COMPLETE) {
            Console.WriteLine("Framebuffer not complete");
        }

        glBindFramebuffer(GL_FRAMEBUFFER, 0);
        AssertNoGlError();
    }

    protected override void OnDetachedFromContext(Context context)
    {
        if (_frameBufferId != 0)
        {
            var frameBufferId = _frameBufferId;
            glDeleteFramebuffers(1, &frameBufferId);
        }
        base.OnDetachedFromContext(context);
    }

    protected override void OnDrawSelf(ICanvas c)
    {
        glBindFramebuffer(GL_READ_FRAMEBUFFER, _frameBufferId);
        AssertNoGlError();

        glBindFramebuffer(GL_DRAW_FRAMEBUFFER, 0);
        AssertNoGlError();

        var position = Position;
        glBlitFramebuffer(
            0, 0, (int)position.Width, (int)position.Height,
            (int)position.Left, (int)position.Bottom, (int)position.Right, (int)position.Top,
            GL_COLOR_BUFFER_BIT,
            GL_LINEAR
        );
    }
}