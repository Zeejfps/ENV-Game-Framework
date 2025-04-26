public struct BorderSizeStyle
{
    public float Left;
    public float Top;
    public float Right;
    public float Bottom;

    public static BorderSizeStyle FromLTRB(float left, float top, float right, float bottom)
    {
        return new BorderSizeStyle
        {
            Left = left,
            Top = top,
            Right = right,
            Bottom = bottom,
        };
    }

    public static BorderSizeStyle All(float size)
    {
        return new BorderSizeStyle
        {
            Left = size,
            Top = size,
            Right = size,
            Bottom = size,
        };
    }
}