using System.Numerics;
using Bricks;
using Bricks.RaylibBackend;


var appBuilder = CreateAppBuilder();
appBuilder.WithWindowName("Brickz");
appBuilder.WithCanvasSize(640, 480);
using var app = appBuilder.Build();

var clock = new StopwatchClock();
var arena = Rectangle.LeftTopWidthHeight(0, 0, 640, 480);
var bricksRepo = new BricksRepo();
SpawnBricks(bricksRepo, arena);
var paddle = new Paddle(app.Input, clock, arena);
var ball = new Ball(clock, arena, paddle, bricksRepo);

clock.Start();
while (!app.IsCloseRequested)
{
    app.Update();
    bricksRepo.Update();
    paddle.Update();
    ball.Update();
    app.Render(paddle, ball, bricksRepo);
    clock.Update();
}


void SpawnBricks(BricksRepo bricksRepo, Rectangle arena)
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
            var brick = new Brick(bricksRepo)
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