using System.Numerics;

namespace NodeGraphApp;

public sealed class SelectionBox
{
    public bool IsVisible { get; set; }
    public Vector2 StartPosition { get; set; }
    public Vector2 EndPosition { get; set; }

    public ScreenRect Bounds
    {
        get
        {
            var endPos = EndPosition;
            var left = StartPosition.X;
            if (endPos.X < left)
                left = endPos.X;
                
            var bottom = StartPosition.Y;
            if (endPos.Y < bottom)
                bottom = endPos.Y;
                
            var width = MathF.Abs(endPos.X - StartPosition.X);
            var height = MathF.Abs(endPos.Y - StartPosition.Y);

            return ScreenRect.FromLBWH(left, bottom, width, height);
        }
    }

    public void Show(Vector2 mousePos)
    {
        StartPosition = mousePos;
        EndPosition = mousePos;
        IsVisible = true;
    }
}