public struct BorderRadiusStyle
{
    public float BottomLeft;
    public float TopLeft;
    public float TopRight;
    public float BottomRight;

    public static BorderRadiusStyle All(float size)
    {
        return new BorderRadiusStyle
        {
            BottomLeft = size,
            TopLeft = size,
            TopRight = size,
            BottomRight = size,
        };
    }
}