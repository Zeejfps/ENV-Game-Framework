using System.Numerics;
using EasyGameFramework.Api;
using EasyGameFramework.Api.Physics;
using static GL46;

namespace Bricks;

public sealed class BricksGame : Game
{
    private const int k_BricksPerRow = 10;
    private const int k_BrickHeight = 20;
    
    private Ball m_Ball;
    private Paddle m_Paddle;
    private readonly Matrix4x4 m_Camera;
    private readonly Brick[] m_Bricks = new Brick[k_BricksPerRow * 4];
    private readonly ISpriteRenderer m_SpriteRenderer;
    
    public BricksGame(IGameContext context, ISpriteRenderer spriteRenderer) : base(context)
    {
        m_Camera = Matrix4x4.CreateOrthographicOffCenter(
            0, 
            Window.ScreenWidth, 
            0, 
            Window.ScreenHeight,
            0.1f, 10f);
        
        m_SpriteRenderer = spriteRenderer;
        m_Ball = new Ball();
        m_Paddle = new Paddle();
    }

    protected override void OnStartup()
    {
        Window.Title = "Brickz";
        
        var viewportWidth = Window.ScreenWidth;
        var rectWidth = viewportWidth / (float)k_BricksPerRow;
        var rectHeight = 20f;
        
        for (var i = 0; i < m_Bricks.Length; i++)
        {
            var x = (i % k_BricksPerRow) * rectWidth;
            var y = Window.ScreenHeight - rectHeight - (i / k_BricksPerRow) * rectHeight;
            var brick = new Brick
            {
                ScreenRect = new Rect
                {
                    BottomLeft = new Vector2(x, y),
                    Width = rectWidth,
                    Height = rectHeight
                },
            };
            m_Bricks[i] = brick;
            m_SpriteRenderer.Add(brick);
        }
        m_SpriteRenderer.Load();
    }

    protected override void OnFixedUpdate()
    {
    }

    protected override void OnUpdate()
    {
        glClear(GL_COLOR_BUFFER_BIT);
        m_SpriteRenderer.Render(m_Camera);
    }

    protected override void OnShutdown()
    {
    }
}