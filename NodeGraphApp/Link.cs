using System.Numerics;
using NodeGraphApp;

public sealed class Link
{
    public Vector2 StartPosition { get; set; }
    public Vector2 EndPosition { get; set; }
    public bool IsHovered { get; set; }
    public bool IsSelected { get; set; }

    public ScreenRect Bounds
    {
        get
        {
            var left = StartPosition.X;
            var right = EndPosition.X;
            if (left > right)
            {
                left = EndPosition.X;
                right = StartPosition.X;
            }

            var bottom = StartPosition.Y;
            var top = EndPosition.Y;
            if (bottom > top)
            {
                bottom = EndPosition.Y;
                top = StartPosition.Y;
            }

            top += 0.5f;
            bottom -= 0.5f;

            return ScreenRect.FromLeftBottomTopRight(left, bottom, top, right);
        }
    }
}