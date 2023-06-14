using System.Numerics;
using EasyGameFramework.Api;
using EasyGameFramework.Api.Cameras;
using EasyGameFramework.Api.Physics;
using EasyGameFramework.Experimental;
using Pong.Physics;

namespace Pong;

public sealed class BallPaddleCollisionSystem
{
    private Physics2D Physics2D { get; }
    private ILogger Logger { get; }
    private OrthographicCamera Camera { get; }
    private IPixelCanvas PixelCanvas { get; }

    public BallPaddleCollisionSystem(IPixelCanvas pixelCanvas, OrthographicCamera camera, Physics2D physics2D, ILogger logger)
    {
        PixelCanvas = pixelCanvas;
        Camera = camera;
        Physics2D = physics2D;
        Logger = logger;
    }

    public void Update(float dt, Ball ball, Paddle bottomPaddle, Paddle topPaddle)
    {
        var topPaddleRect = CreateCollisionRect(topPaddle);
        var botPaddleRect = CreateCollisionRect(bottomPaddle);

        var ray = new Ray2D
        {
            Origin = ball.CurrPosition,
            Direction = ball.Velocity * dt
        };
        
        if (Physics2D.TryRaycastRect(ray, topPaddleRect, out var topHit))
        {
            ball.CurrPosition = topHit.HitPoint;
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

    public void DebugRender(Paddle topPaddle, Paddle bottomPaddle)
    {
        DrawDebugRect(topPaddle);
        DrawDebugRect(bottomPaddle);
    }

    private void DrawDebugRect(Paddle paddle)
    {
        var rect = CreateCollisionRect(paddle);
        var bottomLeftViewportPoint = Camera.WorldToViewportPoint(rect.BottomLeft);
        var topRightViewportPoint = Camera.WorldToViewportPoint(rect.TopRight);
        var canvasX = bottomLeftViewportPoint.X * PixelCanvas.ResolutionX;
        var canvasY = bottomLeftViewportPoint.Y * PixelCanvas.ResolutionY;
        var canvasW = (topRightViewportPoint.X - bottomLeftViewportPoint.X) * PixelCanvas.ResolutionX;
        var canvasH = (topRightViewportPoint.Y - bottomLeftViewportPoint.Y) * PixelCanvas.ResolutionY;
        PixelCanvas.DrawRect((int)canvasX, (int)canvasY,
            (int)canvasW, 
            (int)canvasH);
    }

    private Rect CreateCollisionRect(Paddle paddle)
    {
        return new Rect
        {
            BottomLeft = new Vector2(
                paddle.CurrPosition.X - paddle.Size - 0.5f,
                paddle.CurrPosition.Y - 1f - 0.5f
            ),
            Width = paddle.Size * 2f + 1f,
            Height = 2 + 1f
        };
    }
}