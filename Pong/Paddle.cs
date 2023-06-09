using System.Numerics;
using EasyGameFramework.Api.Physics;

namespace Pong;

public sealed class Paddle
{
    public Vector2 CurrPosition { get; set; }
    public Vector2 PrevPosition { get; set; }
    public float Size { get; } = 10;
    public Rect Bounds { get; set; }
    
    public void MoveLeft(float xDelta)
    {
        PrevPosition = CurrPosition;
        var newPositionX = CurrPosition.X - xDelta;
        if (newPositionX - Size < Bounds.Left)
            newPositionX = Bounds.Left + Size;
        CurrPosition = CurrPosition with { X = newPositionX };
    }

    public void MoveRight(float xDelta)
    {
        PrevPosition = CurrPosition;
        var newPositionX = CurrPosition.X + xDelta;
        if (newPositionX + Size > Bounds.Right)
            newPositionX = Bounds.Right - Size;
        CurrPosition = CurrPosition with { X = newPositionX };
    }
}