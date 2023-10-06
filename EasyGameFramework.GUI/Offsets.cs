namespace OpenGLSandbox;

public struct Offsets
{
    public float Left;
    public float Right;
    public float Top;
    public float Bottom;

    public Offsets(float top, float right, float bottom, float left)
    {
        Bottom = bottom;
        Top = top;
        Right = right;
        Left = left;
    }

    public static Offsets All(float offset)
    {
        return new Offsets(offset, offset, offset, offset);
    }
}