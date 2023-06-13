using System.Numerics;
using EasyGameFramework.Api.Physics;

namespace Pong;

public sealed class Paddle
{
    public Vector2 CurrPosition { get; set; }
    public Vector2 PrevPosition { get; set; }
    public float Size { get; } = 10;
    public Rect LevelBounds { get; set; }
    
    public void MoveLeft(float xDelta)
    {
        var newPositionX = CurrPosition.X - xDelta;
        if (newPositionX - Size < LevelBounds.Left)
            newPositionX = LevelBounds.Left + Size;
        CurrPosition = CurrPosition with { X = newPositionX };
    }

    public void MoveRight(float xDelta)
    {
        var newPositionX = CurrPosition.X + xDelta;
        if (newPositionX + Size > LevelBounds.Right)
            newPositionX = LevelBounds.Right - Size;
        CurrPosition = CurrPosition with { X = newPositionX };
    }
}