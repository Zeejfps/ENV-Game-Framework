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
}