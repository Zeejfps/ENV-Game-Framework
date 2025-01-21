using Bricks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoKeys = Microsoft.Xna.Framework.Input.Keyboard;

namespace Brickz.MonoGame;

public class MonoGameEngine : Game, IEngine
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    private readonly BrickzGame _bricksGame;
    private readonly MonoGameKeyboard _keyboard;
    
    public MonoGameEngine()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = false;
        _bricksGame = new BrickzGame(this);
        _keyboard = new MonoGameKeyboard();
    }

    protected override void Initialize()
    {
        _keyboard.Init();
        _bricksGame.OnStartup();
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        // TODO: use this.Content to load your game content here
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
            MonoKeys.GetState().IsKeyDown(Keys.Escape))
            Exit();

        // TODO: Add your update logic here

        _bricksGame.OnUpdate();
        _keyboard.Update();
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        // TODO: Add your drawing code here

        base.Draw(gameTime);
    }

    public IKeyboard Keyboard => _keyboard;

    public void Render(World world)
    {
        throw new System.NotImplementedException();
    }
}