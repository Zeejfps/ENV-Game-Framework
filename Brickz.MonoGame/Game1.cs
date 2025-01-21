using Bricks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoKeys = Microsoft.Xna.Framework.Input.Keyboard;

namespace Brickz.MonoGame;

public class Game1 : Game, IEngine
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    private BrickzGame _bricksGame;
    
    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = false;
        _bricksGame = new BrickzGame(this);
    }

    protected override void Initialize()
    {
        // TODO: Add your initialization logic here

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

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        // TODO: Add your drawing code here

        base.Draw(gameTime);
    }

    public IKeyboard Keyboard { get; }
    public void Run(IGame game)
    {
        throw new System.NotImplementedException();
    }

    public void Render(World world)
    {
        throw new System.NotImplementedException();
    }
}