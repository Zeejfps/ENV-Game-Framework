using System.Numerics;

namespace Pong;

public sealed class Paddle
{
    public Vector2 CurrPosition { get; set; }
    public Vector2 PrevPosition { get; set; }

    public void MoveLeft(float xDelta)
    {
        PrevPosition = CurrPosition;
        var newPositionX = CurrPosition.X - xDelta;
        if (newPositionX- 10 < -50)
            newPositionX = -40;
        CurrPosition = CurrPosition with { X = newPositionX };
    }

    public void MoveRight(float xDelta)
    {
        PrevPosition = CurrPosition;
        var newPositionX = CurrPosition.X + xDelta;
        if (newPositionX + 10 > 50)
            newPositionX = 40;
        CurrPosition = CurrPosition with { X = newPositionX };
    }
}