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
    
    private readonly Dictionary<Entity, BallSprite> _ballSprites = new();
    
    public Game()
    {
        _sim = new BricksSim();
        //Raylib.SetConfigFlags(ConfigFlags.FLAG_VSYNC_HINT);
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
                foreach (var (entity, spriteComp) in _sim.Sprites.AddedComponents)
                {
                    if (spriteComp.Kind == SpriteKind.Ball)
                    {
                        var ballSprite = new BallSprite(_spriteSheet, 20, 20);
                        _ballSprites.Add(entity, ballSprite);
                    }
                }
                foreach (var (entity, spriteComp) in _sim.Sprites.RemovedComponents)
                {
                    if (spriteComp.Kind == SpriteKind.Ball)
                    {
                        _ballSprites.Remove(entity);
                    }
                }
                _sim.Update(fixedDelta);
                accumulator -= fixedDelta;
            }
            
            Raylib.BeginDrawing();
            Raylib.ClearBackground(new Color(80, 80, 80, 255));

            var lerp = accumulator / fixedDelta;
            foreach (var updatedComponent in _sim.Rigidbodies.UpdatedComponents)
            {
                var entity =  updatedComponent.Entity;
                var prevPos = updatedComponent.PrevValue.Position;
                var currPos = updatedComponent.NewValue.Position;
                if (_ballSprites.TryGetValue(entity, out var sprite))
                {
                    sprite.Pos = Vector2.Lerp(prevPos, currPos, lerp);
                }
            }

            foreach (var sprite in _ballSprites.Values)
            {
                sprite.DrawSelf();
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

class BallSprite
{
    public Vector2 Pos { get; set; }
    
    private Texture _spriteSheet;

    private float _width;
    private float _height;
    private float _halfWidth;
    private float _halfHeight;
    private readonly Color _tint = new(255, 255, 255, 255);


    public BallSprite(Texture spriteSheet, float width, float height)
    {
        _spriteSheet = spriteSheet;
        _width = width;
        _height = height;
        _halfWidth = width * 0.5f;
        _halfHeight = height * 0.5f;
    }
    
    public void DrawSelf()
    {
        var left = Pos.X - _halfWidth;
        var top = Pos.Y + _halfHeight;
        Raylib.DrawTexturePro(_spriteSheet,
            new Rectangle(120, 0, 20, 20),
            new Rectangle(left, top, _width, _height),
            new Vector2(0, 0),
            0, 
            _tint);
    }
}