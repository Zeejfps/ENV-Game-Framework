namespace OpenGLSandbox;

public struct BorderSize
{
    public float Top;
    public float Right;
    public float Bottom;
    public float Left;
        
    public static BorderSize FromTRBL(float top, float right, float bottom, float left)
    {
        return new BorderSize
        {
            Top = top,
            Right = right,
            Bottom = bottom,
            Left = left
        };
    }

    public static BorderSize All(float size)
    {
        return new BorderSize
        {
            Top = size,
            Right = size,
            Bottom = size,
            Left = size
        };
    }
}