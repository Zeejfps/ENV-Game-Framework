using Bricks;
using Bricks.Controllers;

public sealed class BrickzGame
{
    public World World { get; }
    private IFramework Framework { get; }
    private StopwatchClock Clock { get; }
    private PaddleKeyboardController PaddleController { get; }
    private ClockController ClockController { get; }
    
    public BrickzGame(IFramework framework)
    {
        var clock = new StopwatchClock();
        var world = new World(clock);

        Framework = framework;
        Clock = clock;
        World = world;
        PaddleController = new PaddleKeyboardController(framework.Keyboard, world);
        ClockController = new ClockController(clock, framework.Keyboard);

        var paddle = world.CreatePaddle();
        paddle.Spawn();
        
        var ball = world.CreateBall();
        ball.Spawn();

        CreateAndSpawnBricks(world);
    }
    
    public void OnStartup()
    {
        Clock.Start();
    }

    public void OnUpdate()
    {
        var world = World;
        
        Clock.Update();
        
        PaddleController.Update();
        ClockController.Update();
        
        if (Framework.Keyboard.WasKeyPressedThisFrame(KeyCode.Space))
        {
            var newBall = world.CreateBall();
            newBall.Spawn();
        }
    
        World.Update();
    }

    public void OnShutdown()
    {
    }
    
    void CreateAndSpawnBricks(World game)
    {
        var arena = game.Arena;
        var leftPadding = 10;
        var rightPadding = 10;
        var topPadding = 10;
        var horizontalGap = 5;
        var verticalGap = 5;
        var bricksPerRowCount = 8;
        var brickRowsCount = 4;
        var brickHeight = 30;
        var rowWidth = arena.Width - leftPadding - rightPadding - (bricksPerRowCount-1) * horizontalGap;
        var brickWidth = rowWidth / bricksPerRowCount;
        var brickHalfWidth = brickWidth * 0.5f;
        var brickHalfHeight = brickHeight * 0.5f;

        for (var i = 0; i < brickRowsCount; i++)
        {
            var y = (i * brickHeight) + (i * verticalGap) + brickHalfHeight + topPadding;
            for (var j = 0; j < bricksPerRowCount; j++)
            {
                var x = (j * brickWidth) + (j * horizontalGap) + brickHalfWidth + leftPadding;
                var brick = game.CreateBrick(x, y, brickWidth, brickHeight);
                brick.Spawn();
            }
        }
    }
}