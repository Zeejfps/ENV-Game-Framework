using System.Diagnostics;
using System.Numerics;
using Bricks.PhysicsModule;

namespace Bricks.ECS.Raylib;
using Raylib_CsLo;
using Color = Raylib_CsLo.Color;
public sealed class Game
{
    private readonly Texture _spriteSheet;
    private readonly BricksSim _sim;
    private readonly Color _white = new(255, 255, 255, 255);

    private Stopwatch _stopwatch = new();

    public Game()
    {
        _sim = new BricksSim();
        Raylib.SetConfigFlags(ConfigFlags.FLAG_VSYNC_HINT);
        Raylib.InitWindow(640, 480, "Bricks ECS");
        _spriteSheet = Raylib.LoadTexture("Assets/sprite_atlas.png");
    }
    
    public void Run()
    {
        while (!Raylib.WindowShouldClose())
        {
            var dt = _stopwatch.ElapsedMilliseconds / 1000f;
            _stopwatch.Restart();
            Raylib.BeginDrawing();
            Raylib.ClearBackground(new Color(80, 80, 80, 255));
            
            _sim.Update(dt);

            foreach (var entity in _sim.World.Entities)
            {
                if (!_sim.Renderables.TryGetComponent(entity, out var renderable))
                    continue;
                
                if (!_sim.Aabbs.TryGetComponent(entity, out var aabb))
                    continue;

                if (renderable.Kind == RenderableKind.Ball)
                {
                    DrawBallSprite(aabb);
                }
            }

            Raylib.EndDrawing();
        }
    }
    
    private void DrawBallSprite(AABB ballRect)
    {
        Raylib.DrawTexturePro(_spriteSheet,
            new Rectangle(120, 0, 20, 20),
            new Rectangle(ballRect.Left, ballRect.Top, ballRect.Width, ballRect.Height),
            new Vector2(0, 0),
            0, 
            _white);
    }
}