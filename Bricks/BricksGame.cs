using System.Numerics;
using EasyGameFramework.Api;
using EasyGameFramework.Api.Physics;
using static GL46;

namespace Bricks;

public sealed class BricksGame : Game
{
    private const int k_BricksPerRow = 10;
    private const int k_BrickHeight = 20;

    private static readonly Vector3[] BrickColors = {
        HSVtoRGB(90f, 0.5f, 1f),
        HSVtoRGB(120f, 0.5f, 1f),
        HSVtoRGB(140f, 0.5f, 1f),
        HSVtoRGB(160f, 0.5f, 1f),
        HSVtoRGB(180f, 0.5f, 1f),
        HSVtoRGB(200f, 0.5f, 1f),
        HSVtoRGB(220f, 0.5f, 1f),
        HSVtoRGB(240f, 0.5f, 1f),
        HSVtoRGB(260f, 0.5f, 1f),
        HSVtoRGB(280f, 0.5f, 1f),
    };

    private Paddle m_Paddle;
    private readonly Ball m_Ball;
    private readonly Matrix4x4 m_Camera;
    private readonly Brick[] m_Bricks = new Brick[k_BricksPerRow * 4];
    private readonly ISpriteRenderer m_SpriteRenderer;
    
    public BricksGame(IGameContext context, ISpriteRenderer spriteRenderer) : base(context)
    {
        m_Camera = Matrix4x4.CreateOrthographicOffCenter(
            0, 
            Window.ScreenWidth, 
            Window.ScreenHeight, 
            0,
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
        var rectHeight = k_BrickHeight;
        
        for (var i = 0; i < m_Bricks.Length; i++)
        {
            var x = (i % k_BricksPerRow) * rectWidth;
            var y = (i / k_BricksPerRow) * rectHeight;
            var brick = new Brick
            {
                ScreenRect = new ScreenRect
                {
                    X = x,
                    Y = y,
                    Width = rectWidth,
                    Height = rectHeight
                },
                Color = BrickColors[i % BrickColors.Length],
            };
            m_Bricks[i] = brick;
            m_SpriteRenderer.Add(brick);
        }
        
        m_SpriteRenderer.Add(m_Ball);
        m_SpriteRenderer.Load();
    }

    protected override void OnFixedUpdate()
    {
    }

    protected override void OnUpdate()
    {
        m_Ball.Move(Time.UpdateDeltaTime);
        
        glClear(GL_COLOR_BUFFER_BIT);
        m_SpriteRenderer.Render(m_Camera);
    }

    protected override void OnShutdown()
    {
    }
    
    public static Vector3 HSVtoRGB(float h, float s, float v)
    {
        if (s == 0) // Achromatic (grey)
            return new Vector3(v, v, v);

        h = h / 60f;
        int i = (int)Math.Floor(h);
        float f = h - i;
        float p = v * (1 - s);
        float q = v * (1 - s * f);
        float t = v * (1 - s * (1 - f));

        float r, g, b;
        switch (i)
        {
            case 0:
                r = v; g = t; b = p;
                break;
            case 1:
                r = q; g = v; b = p;
                break;
            case 2:
                r = p; g = v; b = t;
                break;
            case 3:
                r = p; g = q; b = v;
                break;
            case 4:
                r = t; g = p; b = v;
                break;
            default: // case 5:
                r = v; g = p; b = q;
                break;
        }

        return new Vector3(r, g, b);
    }
}