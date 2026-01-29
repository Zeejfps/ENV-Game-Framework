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

                    var spriteView = CreateSpriteView(spriteComp);
                    AddSpriteView(spriteView, entity);
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
            }

            foreach (var sprite in _sprites.Values)
            {
                sprite.DrawSelf();
            }

            Raylib.EndDrawing();
        }
    }

    private ISprite CreateSpriteView(Sprite sprite)
    {
        return sprite.Kind switch
        {
            SpriteKind.Ball => new BallSprite(_spriteSheet, sprite.Width, sprite.Height),
            SpriteKind.Brick => new BrickSprite(_spriteSheet, sprite.Width, sprite.Height),
            SpriteKind.Paddle => new PaddleSprite(_spriteSheet, sprite.Width, sprite.Height),
            _ => throw new ArgumentOutOfRangeException()
        };
    }
    
    private void AddSpriteView(ISprite sprite, Entity entity)
    {
        _sprites.Add(entity, sprite);   
        if (_sim.Transforms.TryGetComponent(entity, out var t))
            sprite.Pos = t.Position;
    }
}