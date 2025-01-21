using System.Numerics;
using Bricks;
using Bricks.Entities;
using Bricks.RaylibBackend;

using var app = CreateAppBuilder()
    .WithWindowName("Brickz")
    .WithCanvasSize(640, 480)
    .Build();

var arena = AABB.FromLeftTopWidthHeight(0, 0, 640, 480);
var clock = new StopwatchClock();
var game = new Game(clock);

var controller = new KeyboardController(app.Keyboard, game);

var paddle = new PaddleEntity(game, arena);
paddle.Spawn();

var ball = new BallEntity(arena, game);
ball.Spawn();

SpawnBricks(game, arena);

clock.Start();
while (!app.IsCloseRequested)
{
    app.Update();

    controller.Update();
    if (app.Keyboard.WasKeyPressedThisFrame(KeyCode.Space))
    {
        var newBall = new BallEntity(arena, game);
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

void SpawnBricks(Game world, AABB arena)
{
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
            var brick = new BrickEntity(world)
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