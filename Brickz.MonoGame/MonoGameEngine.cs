using System.IO;
using Bricks;
using Bricks.Archetypes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoKeys = Microsoft.Xna.Framework.Input.Keyboard;

namespace Brickz.MonoGame;

public class MonoGameEngine : Game, IEngine
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private Texture2D _spriteSheet;
    
    private readonly BrickzGame _game;
    private readonly MonoGameKeyboard _keyboard;
    private readonly Color _backgroundColor = new(80, 80, 80, 255);
    private readonly Color _brickColor = new(0, 121, 241, 255);
    
    public MonoGameEngine()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = false;
        _keyboard = new MonoGameKeyboard();
        _game = new BrickzGame(this);
    }

    protected override void Initialize()
    {
        _graphics.IsFullScreen = false;
        _graphics.SynchronizeWithVerticalRetrace = true;
        _graphics.PreferredBackBufferWidth = 640;
        _graphics.PreferredBackBufferHeight = 480;
        _graphics.ApplyChanges();
        
        _keyboard.Init();
        _game.OnStartup();
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _spriteSheet = Content.Load<Texture2D>("sprite_atlas");
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
            MonoKeys.GetState().IsKeyDown(Keys.Escape))
        {
            Exit();
        }
        else
        {
            _game.OnUpdate();
            _keyboard.Update();
        }
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(_backgroundColor);

        _spriteBatch.Begin(
            SpriteSortMode.Deferred,
            BlendState.AlphaBlend,
            SamplerState.PointClamp
        );

        DrawBricks();
        DrawBalls();
        DrawPaddle();
        
        _spriteBatch.End();

        base.Draw(gameTime);
    }

    private void DrawBricks()
    {
        var bricks = _game.World.Bricks.GetAll();
        foreach (var brick in bricks)
        {
            if (brick.IsDamaged)
            {
                DrawDamagedBrick(brick);
            }
            else
            {
                DrawNormalBrick(brick);
            }
        }
    }

    private void DrawDamagedBrick(IBrick brick)
    {
        var aabb = brick.GetAABB();
        var sourceRect = new Rectangle(0, 40, 60, 20);
        var destinationRect = new Rectangle((int)aabb.Left, (int)aabb.Top, (int)aabb.Width, (int)aabb.Height);
        _spriteBatch.Draw(_spriteSheet, destinationRect, sourceRect, _brickColor);
    }
    
    private void DrawNormalBrick(IBrick brick)
    {
        var aabb = brick.GetAABB();
        var sourceRect = new Rectangle(0, 20, 60, 20);
        var destinationRect = new Rectangle((int)aabb.Left, (int)aabb.Top, (int)aabb.Width, (int)aabb.Height);
        _spriteBatch.Draw(_spriteSheet, destinationRect, sourceRect, _brickColor);
    }

    private void DrawPaddle()
    {
        var paddle = _game.World.Paddle;
        var aabb = paddle.GetAABB();
        var sourceRect = new Rectangle(0, 0, 120, 19);
        var destinationRect = new Rectangle((int)aabb.Left, (int)aabb.Top, (int)aabb.Width, (int)aabb.Height);
        _spriteBatch.Draw(_spriteSheet, destinationRect, sourceRect, Color.White);
    }
    
    private void DrawBalls()
    {
        var balls = _game.World.Balls.GetAll();
        foreach (var ball in balls)
        {
            var aabb = ball.GetAABB();
            var sourceRect = new Rectangle(120, 0, 20, 20);
            var destinationRect = new Rectangle((int)aabb.Left, (int)aabb.Top, (int)aabb.Width, (int)aabb.Height);
            _spriteBatch.Draw(_spriteSheet, destinationRect, sourceRect, Color.White);
        }
    }

    public IKeyboard Keyboard => _keyboard;
}