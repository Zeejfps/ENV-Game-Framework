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
    
    public void Run()
    {
        while (!Raylib.WindowShouldClose())
        {
            Raylib.BeginDrawing();
            Raylib.ClearBackground(new Color(80, 80, 80, 255));
            
            _sim.Update(0.1f);

            foreach (var entity in _sim.World.Entities)
            {
                if (!_sim.Renderables.TryGetComponent(entity, out var renderable))
                    continue;

                if (renderable.Kind == RenderableKind.Ball)
                {
                    
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