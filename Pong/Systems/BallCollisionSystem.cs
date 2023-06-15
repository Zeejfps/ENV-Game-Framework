using System.Numerics;
using EasyGameFramework.Api;
using EasyGameFramework.Api.Cameras;
using EasyGameFramework.Api.Physics;
using EasyGameFramework.Experimental;
using Pong.Physics;

namespace Pong;

public sealed class BallCollisionSystem
{
    private Physics2D Physics2D { get; }
    private ILogger Logger { get; }
    private OrthographicCamera Camera { get; }
    private IPixelCanvas PixelCanvas { get; }
    
    private readonly List<IPhysicsEntity> m_Bodies = new();
    private readonly List<IBoxCollider> m_RectColliders = new();

    private readonly Rect[] m_Colliders = new Rect[5000];
    private readonly PhysicsEntity[] m_PhysicsEntities = new PhysicsEntity[5000];
    private Rect Bounds { get; }

    public BallCollisionSystem(IPixelCanvas pixelCanvas, OrthographicCamera camera, Physics2D physics2D, ILogger logger, Rect bounds)
    {
        PixelCanvas = pixelCanvas;
        Camera = camera;
        Physics2D = physics2D;
        Logger = logger;
        Bounds = bounds;
    }

    public void AddEntity(IPhysicsEntity body)
    {
        m_Bodies.Add(body);
    }

    public void AddCollider(IBoxCollider collider)
    {
        m_RectColliders.Add(collider);
    }

    public void Update(float dt)
    {
        var colliders = m_Colliders.AsSpan();
        var colliderCount = m_RectColliders.Count;
        for (var i = 0; i < colliderCount; i++)
        {
            colliders[i] = m_RectColliders[i].AABB;
        }

        var entities = m_PhysicsEntities.AsSpan();
        var entityCount = m_Bodies.Count;
        for (var i = 0; i < entityCount; i++)
        {
            var body = m_Bodies[i];
            entities[i] = body.Save();
        }
        
        for (var i = 0; i < colliderCount; i++)
        {
            ref var collider = ref colliders[i];
            for (var j = 0; j < entityCount; j++)
            {
                ref var entity = ref entities[j];
                var ray = new Ray2D
                {
                    Origin = entity.Position,
                    Direction = entity.Velocity * dt,
                };

                var newPosition = entity.Position + entity.Velocity * dt;
                if (newPosition.X < Bounds.Left)
                {
                    newPosition.X = Bounds.Left;
                    entity.Velocity = entity.Velocity with { X = -entity.Velocity.X };
                }
                else if (newPosition.X > Bounds.Right)
                {
                    newPosition.X = Bounds.Right;
                    entity.Velocity = entity.Velocity with { X = -entity.Velocity.X };
                }
                if (newPosition.Y < Bounds.Bottom)
                {
                    newPosition.Y = Bounds.Bottom;
                    entity.Velocity = entity.Velocity with { Y = -entity.Velocity.Y };
                }
                else if (newPosition.Y > Bounds.Top)
                {
                    newPosition.Y = Bounds.Top;
                    entity.Velocity = entity.Velocity with { Y = -entity.Velocity.Y };
                }
                else if (Physics2D.TryRaycastRect(ray, collider, out var hit))
                {
                    newPosition = hit.HitPoint + hit.Normal;
                    entity.Velocity = entity.Velocity with { Y = -entity.Velocity.Y };
                }

                entity.Position = newPosition;
            }
        }
        
        for (var i = 0; i < entityCount; i++)
        {
            var body = m_Bodies[i];
            body.Load(entities[i]);
        }
    }

    public void DebugRender(Ball ball, Paddle topPaddle, Paddle bottomPaddle)
    {
        var resolutionX = PixelCanvas.ResolutionX;
        var resolutionY = PixelCanvas.ResolutionY;
        var velocityEnd = Camera.WorldToViewportPoint(ball.Position + ball.Velocity) *
                          new Vector2(resolutionX, resolutionY);
        
        var ballCenter = Camera.WorldToViewportPoint(ball.Position);
        var botLeft = Camera.WorldToViewportPoint(ball.Position - new Vector2(1f, 1f));
        var topRight = Camera.WorldToViewportPoint(ball.Position + Vector2.One);
        var canvasX = (int)(botLeft.X * PixelCanvas.ResolutionX);
        var canvasY = (int)(botLeft.Y * PixelCanvas.ResolutionY);
        var canvasW = (topRight.X - botLeft.X) * PixelCanvas.ResolutionX;
        var canvasH = (topRight.Y - botLeft.Y) * PixelCanvas.ResolutionY;

        var centerX = (int)(ballCenter.X * PixelCanvas.ResolutionX);
        var centerY = (int)(ballCenter.Y * resolutionY);
        
        //Logger.Trace($"BL: {ballCenter}, TP: {topRight}");
        //Logger.Trace($"Canvas: X {canvasX}, Y: {canvasY}, W: {canvasW}, H: {canvasH}");
        PixelCanvas.DrawRect((int)canvasX, (int)canvasY, (int)canvasW, (int)canvasH);
        PixelCanvas.DrawLine(centerX, centerY, (int)velocityEnd.X, (int)velocityEnd.Y);
        
        DrawDebugRect(topPaddle);
        DrawDebugRect(bottomPaddle);
    }

    private void DrawDebugRect(Paddle paddle)
    {
        var rect = paddle.AABB;
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
}