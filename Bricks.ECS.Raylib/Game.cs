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
    private readonly Dictionary<Entity, BrickSprite> _brickSprites = new();
    
    public Game()
    {
        _sim = new BricksSim(AABB.FromLeftTopWidthHeight(0, 0, 640, 480));
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
            if (Raylib.IsKeyPressed(KeyboardKey.KEY_A))
            {
                _sim.SpawnBall();
            }
            
            var frameTime = Raylib.GetFrameTime();
            accumulator += frameTime;
            
            while (accumulator >= fixedDelta)
            {
                foreach (var (entity, spriteComp) in _sim.Sprites.AddedComponents)
                {
                    if (spriteComp.Kind == SpriteKind.Ball)
                    {
                        var ballSprite = new BallSprite(_spriteSheet, spriteComp.Width, spriteComp.Height);
                        _ballSprites.Add(entity, ballSprite);
                    }
                    else if (spriteComp.Kind == SpriteKind.Brick)
                    {
                        var brickSprite = new BrickSprite(_spriteSheet, spriteComp.Width, spriteComp.Height);
                        _brickSprites.Add(entity, brickSprite);
                    }
                }
                foreach (var (entity, spriteComp) in _sim.Sprites.RemovedComponents)
                {
                    if (spriteComp.Kind == SpriteKind.Ball)
                    {
                        _ballSprites.Remove(entity);
                    }
                    else if (spriteComp.Kind == SpriteKind.Brick)
                    {
                        _brickSprites.Remove(entity);
                    }
                }
                _sim.Update(fixedDelta);
                accumulator -= fixedDelta;
            }
            
            Raylib.BeginDrawing();
            Raylib.ClearBackground(new Color(80, 80, 80, 255));
            
            var lerp = accumulator / fixedDelta;
            foreach (var entity in _sim.World.Entities)
            {
                if (_sim.Rigidbodies.WasUpdated(entity, out var updatedComponent))
                {
                    var prevPos = updatedComponent.PrevValue.Position;
                    var currPos = updatedComponent.NewValue.Position;
                    if (_ballSprites.TryGetValue(entity, out var sprite))
                    {
                        sprite.Pos = Vector2.Lerp(prevPos, currPos, lerp);
                    }
                }
                else if (_sim.Rigidbodies.TryGetComponent(entity, out var rb))
                {
                    if (_brickSprites.TryGetValue(entity, out var sprite))
                    {
                        sprite.Pos = rb.Position;
                    }
                }
            }

            foreach (var sprite in _ballSprites.Values)
            {
                sprite.DrawSelf();
            }

            foreach (var brickSprite in _brickSprites.Values)
            {
                brickSprite.DrawSelf();
            }

            Raylib.EndDrawing();
        }
    }
}

class BrickSprite
{
    public Vector2 Pos { get; set; }
    
    private readonly Texture _spriteSheet;

    private readonly float _width;
    private readonly float _height;
    private readonly Color _tint = new(255, 255, 255, 255);

    public BrickSprite(Texture spriteSheet, float width, float height)
    {
        _spriteSheet = spriteSheet;
        _width = width;
        _height = height;
    }

    public void DrawSelf()
    {
        DrawNormalBrickSprite();

        // var brickRect = brick.GetAABB();
        // if (brick.IsDamaged)
        // {
        //     DrawDamagedBrickSprite(brickRect, _brickColor);
        // }
        // else
        // {
        //     DrawNormalBrickSprite(brickRect, _brickColor);
        // }
    }
    
    private void DrawNormalBrickSprite()
    {
        Raylib.DrawTexturePro(_spriteSheet,
            new Rectangle(0, 20, 60, 20),
            new Rectangle(Pos.X- _width * 0.5f, Pos.Y - _height * 0.5f, _width, _height),
            new Vector2(0, 0),
            0, 
            _tint);
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