using System.Numerics;
using Bricks.ECS.Components;
using Bricks.PhysicsModule;

namespace Bricks.ECS.Raylib;
using Raylib_CsLo;
using Color = Raylib_CsLo.Color;
public sealed class Game
{
    private readonly Texture _spriteSheet;
    private readonly BricksSim _sim;
    private readonly Dictionary<Entity, ISprite> _sprites = new();
    
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
            if (Raylib.IsKeyPressed(KeyboardKey.KEY_SPACE))
            {
                _sim.SpawnBall();
            }
            
            _sim.MovePaddleLeft = Raylib.IsKeyDown(KeyboardKey.KEY_A);
            _sim.MovePaddleRight = Raylib.IsKeyDown(KeyboardKey.KEY_D);
            
            var frameTime = Raylib.GetFrameTime();
            accumulator += frameTime;
            
            while (accumulator >= fixedDelta)
            {
                foreach (var entity in _sim.World.SpawnedEntities)
                {
                    if (!_sim.Sprites.TryGetComponent(entity, out var spriteComp))
                        continue;
                    
                    if (spriteComp.Kind == SpriteKind.Ball)
                    {
                        var ballSprite = new BallSprite(_spriteSheet, spriteComp.Width, spriteComp.Height);
                        _sprites.Add(entity, ballSprite);
                    }
                    else if (spriteComp.Kind == SpriteKind.Brick)
                    {
                        var brickSprite = new BrickSprite(_spriteSheet, spriteComp.Width, spriteComp.Height);
                        _sprites.Add(entity, brickSprite);
                    }
                    else if (spriteComp.Kind == SpriteKind.Paddle)
                    {
                        var paddleSprite = new PaddleSprite(_spriteSheet, spriteComp.Width, spriteComp.Height);
                        _sprites.Add(entity, paddleSprite);   
                    }
                }
                foreach (var entity in _sim.World.DespawnedEntities)
                {
                    if (!_sim.Sprites.TryGetComponent(entity, out var spriteComp))
                        continue;
                    
                    _sprites.Remove(entity);
                }
                _sim.Update(fixedDelta);
                accumulator -= fixedDelta;
            }
            
            Raylib.BeginDrawing();
            Raylib.ClearBackground(new Color(80, 80, 80, 255));
            
            var lerp = accumulator / fixedDelta;
            foreach (var entity in _sim.World.Entities)
            {
                if (_sim.Transforms.WillUpdate(entity, out var updatedComponent))
                {
                    var prevPos = updatedComponent.PrevValue.Position;
                    var currPos = updatedComponent.NewValue.Position;
                    if (_sprites.TryGetValue(entity, out var sprite))
                    {
                        sprite.Pos = Vector2.Lerp(prevPos, currPos, lerp);
                    }
                }
                else if (_sim.Transforms.TryGetComponent(entity, out var rb))
                {
                    if (_sprites.TryGetValue(entity, out var sprite))
                    {
                        sprite.Pos = rb.Position;
                    }
                }
            }

            foreach (var sprite in _sprites.Values)
            {
                sprite.DrawSelf();
            }

            Raylib.EndDrawing();
        }
    }
}

class PaddleSprite : ISprite
{
    private readonly Texture _spriteSheet;
    private readonly float _width;
    private readonly float _height;
    private readonly Color _tint = new(255, 255, 255, 255);

    public PaddleSprite(Texture spriteSheet, float width, float height)
    {
        _spriteSheet = spriteSheet;
        _width = width;
        _height = height;
    }

    public void DrawSelf()
    {
        Raylib.DrawTexturePro(_spriteSheet,
            new Rectangle(0, 0, 120, 19),
            new Rectangle(Pos.X - _width * 0.5f, Pos.Y - _height * 0.5f, _width, _height),
            new Vector2(0, 0),
            0, 
            _tint);
    }

    public Vector2 Pos { get; set; }
}

class BrickSprite : ISprite
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

interface ISprite
{
    void DrawSelf();
    Vector2 Pos { get; set; }
}

class BallSprite : ISprite
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