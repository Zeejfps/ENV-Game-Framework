namespace OpenGLSandbox;

public struct Glyph
{
    [VertexAttrib(4, GL46.GL_FLOAT)]
    public Rect ScreenRect;
    [VertexAttrib(4, GL46.GL_FLOAT)]
    public Rect TextureRect;
    [VertexAttrib(4, GL46.GL_FLOAT)]
    public Color Color;
}