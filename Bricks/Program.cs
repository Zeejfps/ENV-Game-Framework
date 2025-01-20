using System.Numerics;
using Bricks;
using Bricks.RaylibBackend;


var appBuilder = CreateAppBuilder();
appBuilder.WithWindowName("Brickz");
appBuilder.WithCanvasSize(640, 480);
using var app = appBuilder.Build();

var clock = new StopwatchClock();
var arena = Rectangle.LeftTopWidthHeight(0, 0, 640, 480);
var bricks = CreateBricks(arena);
var paddle = new Paddle(app.Input, clock, arena);
var ball = new Ball(clock, arena, paddle);

clock.Start();
while (!app.IsCloseRequested)
{
    app.Update();
    paddle.Update();
    ball.Update();
    app.Render(paddle, ball, bricks);
    clock.Update();
}


Brick[] CreateBricks(Rectangle arena)
{
    var leftPadding = 10;
    var rightPadding = 10;
    var topPadding = 10;
    var horizontalGap = 5;
    var verticalGap = 5;
    var bricksPerRowCount = 8;
    var brickRowsCount = 4;
    var brickHeight = 30;
    var bricks = new Brick[bricksPerRowCount * brickRowsCount];
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
            var index = j + i * bricksPerRowCount;
            bricks[index] = new Brick
            {
                Position = new Vector2(x, y),
                Width = brickWidth,
                Height = brickHeight,
            };
        }
    }
    
    return bricks;
}

IAppBuilder CreateAppBuilder()
{
    return new RaylibAppBuilder();
}