using System.Numerics;

namespace Bricks;

public struct Rectangle
{
    public Vector2 Center { get; private set; }
    public float Width { get; private set; }
    public float Height { get; private set; }
    public float Left { get; private init; }
    public float Right { get; private set; }
    public float Top { get; private set; }
    public float Bottom { get; private set; }
    
    public static Rectangle LeftTopWidthHeight(float left, float top, int width, int height)
    {
        var halfWith = width * 0.5f;
        var halfHeight = height * 0.5f;
        return new Rectangle
        {
            Left = left,
            Top = top,
            Bottom = top + height,
            Right = left + width,
            Width = width,
            Height = height,
            Center = new Vector2(left + halfWith, top + halfHeight),
        };
    }
}

public sealed class Paddle
{
    public float HorizontalVelocity { get; private set; }

    // NOTE(Zee): <0, 0> Should be in the dead center of the screen
    public Vector2 CenterPosition { get; private set; }
    public int Width { get; private set; }
    public int Height { get; private set; }
    public const float MaxMovementSpeed = 50f;
    
    private IInput Input { get; }
    private IClock Clock { get; }
    private Rectangle ArenaBounds { get; }
    
    public Paddle(IInput input, IClock clock, Rectangle arenaBounds)
    {
        Input = input;
        Clock = clock;
        ArenaBounds = arenaBounds;
        CenterPosition = new Vector2(ArenaBounds.Center.X, ArenaBounds.Bottom - 50);
        Width = 100;
        Height = 25;
    }

    public void Update()
    {
        UpdateHorizontalVelocity();
        UpdatePosition();
        CheckAndResolveCollision();
    }

    private void UpdateHorizontalVelocity()
    {
        HorizontalVelocity = 0;
        if (Input.IsKeyPressed(KeyCode.A))
        {
            HorizontalVelocity -= MaxMovementSpeed;
        }
        if (Input.IsKeyPressed(KeyCode.D))
        {
            HorizontalVelocity += MaxMovementSpeed;
        }
    }

    private void UpdatePosition()
    {
        CenterPosition += Vector2.UnitX * HorizontalVelocity * Clock.DeltaTimeSeconds;
    }

    private void CheckAndResolveCollision()
    {
        CheckAndResolveCanvasCollision();
    }

    private void CheckAndResolveCanvasCollision()
    {
        var bounds = CalculateBoundsRectangle();
        if (bounds.Left < ArenaBounds.Left)
        {
            CenterPosition -= Vector2.UnitX * bounds.Left;
        }
        else if (bounds.Right > ArenaBounds.Right)
        {
            CenterPosition -= Vector2.UnitX * bounds.Right;
        }
    }

    private Rectangle CalculateBoundsRectangle()
    {
        var halfWidth = Width * 0.5f;
        var halfHeight = Height * 0.5f;
        var x = CenterPosition.X - halfWidth;
        var y = CenterPosition.Y - halfHeight;
        return Rectangle.LeftTopWidthHeight(x, y, Width, Height);
    }
}