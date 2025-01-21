using Bricks;
using Bricks.Controllers;

public enum GameState
{
    Paused,
    Playing,
    Victory,
    Defeat
}

public sealed class BrickzGame
{
    public World World { get; }
    public GameState State { get; private set; }
    
    private IFramework Framework { get; }
    private StopwatchClock Clock { get; }
    private PaddleKeyboardController PaddleController { get; }
    private ClockController ClockController { get; }
    
    public BrickzGame(IFramework framework)
    {
        Framework = framework;
        
        var clock = new StopwatchClock();
        var world = new World(clock);

        Clock = clock;
        World = world;
        PaddleController = new PaddleKeyboardController(world, framework.Keyboard);
        ClockController = new ClockController(clock, framework.Keyboard);
 
        CreateAndSpawnPaddle();
        CreateAndSpawnBall();
        CreateAndSpawnBricks();
        
        State = GameState.Playing;
    }
    
    public void OnStartup()
    {
        Clock.Start();
    }

    public void OnUpdate()
    {
        Clock.Update();
        PaddleController.Update();
        //ClockController.Update();
        
        if (Framework.Keyboard.WasKeyPressedThisFrame(KeyCode.Space))
        {
            var newBall = World.CreateBall();
            newBall.Spawn();
        }
        
        if (Framework.Keyboard.WasKeyPressedThisFrame(KeyCode.P))
        {
            if (State == GameState.Playing)
            {
                Pause();
            }
            else if (State == GameState.Paused)
            {
                Resume();
            }
        }
        
        if (State != GameState.Playing)
        {
            return;
        }
    
        World.Update();
        
        var hasBricks = World.Bricks.GetAll().Any();
        if (!hasBricks)
        {
            Clock.Stop();
            State = GameState.Victory;
        }
    }

    public void Restart()
    {
        State = GameState.Playing;

        foreach (var ball in World.Balls.GetAll())
            ball.Despawn();
        
        CreateAndSpawnBall();
        CreateAndSpawnBricks();
        World.Paddle.Reset();
        Clock.Start();
    }
    
    public void OnShutdown()
    {
    }

    private void CreateAndSpawnPaddle()
    {
        var paddle = World.CreatePaddle();
        paddle.Spawn();
    }

    private void CreateAndSpawnBall()
    {
        var ball = World.CreateBall();
        ball.Spawn();
    }
    
    private void CreateAndSpawnBricks()
    {
        var arena = World.Arena;
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
                var brick = World.CreateBrick(x, y, brickWidth, brickHeight);
                brick.Spawn();
            }
        }
    }

    public void Resume()
    {
        if (State != GameState.Paused)
            return;
        
        Clock.Start();
        State = GameState.Playing;
    }

    public void Pause()
    {
        if (State != GameState.Playing)
            return;

        Clock.Stop();
        State = GameState.Paused;
    }
}