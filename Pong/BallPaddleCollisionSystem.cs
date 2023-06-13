using System.Numerics;
using EasyGameFramework.Api;
using EasyGameFramework.Api.Physics;
using Pong.Physics;

namespace Pong;

public sealed class BallPaddleCollisionSystem
{
    private Physics2D Physics2D { get; }
    private ILogger Logger { get; }

    public BallPaddleCollisionSystem(Physics2D physics2D, ILogger logger)
    {
        Physics2D = physics2D;
        Logger = logger;
    }

    public void Update(float dt, Ball ball, Paddle bottomPaddle, Paddle topPaddle)
    {
        var topPaddleRect = new Rect
        {
            BottomLeft = new Vector2(
                topPaddle.CurrPosition.X - topPaddle.Size - 0.5f,
                topPaddle.CurrPosition.Y - 1f
            ),
            Width = topPaddle.Size * 2f + 1f,
            Height = 2f
        };
        
        var botPaddleRect = new Rect
        {
            BottomLeft = new Vector2(
                bottomPaddle.CurrPosition.X - bottomPaddle.Size - 0.5f,
                bottomPaddle.CurrPosition.Y - 1f
            ),
            Width = bottomPaddle.Size * 2f + 1f,
            Height = 2f
        };

        var ray = new Ray2D
        {
            Origin = ball.CurrPosition,
            Direction = ball.Velocity * dt
        };
        
        if (Physics2D.TryRaycastRect(ray, topPaddleRect, out var topHit))
        {
            ball.CurrPosition = topHit.HitPoint + topHit.Normal;
            ball.Velocity = ball.Velocity with { Y = -ball.Velocity.Y };
            return;
        }

        if (Physics2D.TryRaycastRect(ray, botPaddleRect, out var botHit))
        {
            ball.CurrPosition = botHit.HitPoint + botHit.Normal;
            ball.Velocity = ball.Velocity with { Y = -ball.Velocity.Y };
            return;
        }
    }
}