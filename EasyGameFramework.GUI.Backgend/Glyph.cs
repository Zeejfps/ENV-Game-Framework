namespace OpenGLSandbox;

public struct Glyph
{
    [InstancedAttrib(4, GL46.GL_FLOAT)]
    public Rect ScreenRect;
    [InstancedAttrib(4, GL46.GL_FLOAT)]
    public Rect TextureRect;
    [InstancedAttrib(4, GL46.GL_FLOAT)]
    public Color Color;
}