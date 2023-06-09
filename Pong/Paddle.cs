using System.Numerics;

namespace Pong;

public sealed class Paddle
{
    public Vector2 CurrPosition { get; set; }
    public Vector2 PrevPosition { get; set; }
}