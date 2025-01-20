using System.Numerics;
using Bricks;
using Bricks.Entities;
using Bricks.RaylibBackend;

var appBuilder = CreateAppBuilder();
appBuilder.WithWindowName("Brickz");
appBuilder.WithCanvasSize(640, 480);
using var app = appBuilder.Build();

var arena = Rectangle.LeftTopWidthHeight(0, 0, 640, 480);
var clock = new StopwatchClock();
var world = new World(clock);

var paddle = new PaddleEntity(app.Input, world, arena);
paddle.Spawn();

var ball = new BallEntity(arena, world);
ball.Spawn();

SpawnBricks(world, arena);

clock.Start();
while (!app.IsCloseRequested)
{
    app.Update();

    if (app.Input.WasKeyPressedThisFrame(KeyCode.Space))
    {
        var newBall = new BallEntity(arena, world);
        newBall.Spawn();
    }
    
    world.Update();
    app.Render(world);
    clock.Update();
}

return;

void SpawnBricks(World world, Rectangle arena)
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