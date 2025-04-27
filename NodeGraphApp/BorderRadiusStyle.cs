public struct BorderRadiusStyle
{
    public float Left;
    public float Top;
    public float Right;
    public float Bottom;

    public static BorderRadiusStyle FromLTRB(float left, float top, float right, float bottom)
    {
        return new BorderRadiusStyle
        {
            Left = left,
            Top = top,
            Right = right,
            Bottom = bottom,
        };
    }

    public static BorderRadiusStyle All(float size)
    {
        return new BorderRadiusStyle
        {
            Left = size,
            Top = size,
            Right = size,
            Bottom = size,
        };
    }
}