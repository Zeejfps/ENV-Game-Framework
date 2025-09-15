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
    
    public Game()
    {
        _sim = new BricksSim();
        Raylib.SetConfigFlags(ConfigFlags.FLAG_VSYNC_HINT);
        Raylib.InitWindow(640, 480, "Bricks ECS");
        _spriteSheet = Raylib.LoadTexture("Assets/sprite_atlas.png");
    }
    
    const float fixedDelta = 1f / 60f;
    float accumulator = 0f;
    
    public void Run()
    {
        while (!Raylib.WindowShouldClose())
        {
            var frameTime = Raylib.GetFrameTime();
            accumulator += frameTime;
            
            while (accumulator >= fixedDelta)
            {
                _sim.Update(fixedDelta);
                accumulator -= fixedDelta;
            }
            
            Raylib.BeginDrawing();
            Raylib.ClearBackground(new Color(80, 80, 80, 255));

            foreach (var entity in _sim.World.Entities)
            {
                if (!_sim.Sprites.TryGetComponent(entity, out var renderable))
                    continue;
                
                if (!_sim.Aabbs.TryGetComponent(entity, out var aabb))
                    continue;
                
                var lerp = accumulator / fixedDelta;
                if (renderable.Kind == SpriteKind.Ball)
                {
                    var left = aabb.Left;
                    var top = aabb.Top;
                    DrawBallSprite(AABB.FromLeftTopWidthHeight(left, top, aabb.Width, aabb.Height));
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