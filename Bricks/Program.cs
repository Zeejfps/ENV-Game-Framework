using System.Numerics;
using Bricks;
using Bricks.Entities;
using Bricks.RaylibBackend;

using var app = CreateAppBuilder()
    .WithWindowName("Brickz")
    .WithCanvasSize(640, 480)
    .Build();

var clock = new StopwatchClock();
var game = new Game(clock);

var paddleKeyboardController = new PaddleKeyboardController(app.Keyboard, game);

var paddle = new PaddleEntity(game);
paddle.Spawn();

var ball = new BallEntity(game);
ball.Spawn();

SpawnBricks(game);

clock.Start();
while (!app.IsCloseRequested)
{
    app.Update();

    paddleKeyboardController.Update();
    if (app.Keyboard.WasKeyPressedThisFrame(KeyCode.Space))
    {
        var newBall = new BallEntity(game);
        newBall.Spawn();
    }
    
    if (app.Keyboard.WasKeyPressedThisFrame(KeyCode.P))
    {
        if (clock.IsRunning)
            clock.Stop();
        else
            clock.Start();
    }
    
    if (app.Keyboard.WasKeyPressedThisFrame(KeyCode.L))
    {
        clock.StepForward();
    }
    
    game.Update();
    app.Render(game);
    clock.Update();
}

return;

void SpawnBricks(Game game)
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
            var brick = new BrickEntity(game)
            {
                Position = new Vector2(x, y),
                Width = brickWidth,
                Height = brickHeight,
            };
            brick.Spawn();
        }
    }
}

IAppBuilder CreateAppBuilder()
{
    return new RaylibAppBuilder();
}