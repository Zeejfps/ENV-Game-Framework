using Bricks;
using Bricks.Controllers;
using Bricks.RaylibBackend;

using var app = CreateAppBuilder()
    .WithWindowName("Brickz")
    .WithCanvasSize(640, 480)
    .Build();

var clock = new StopwatchClock();
var game = new Game(clock);
var paddleController = new PaddleKeyboardController(app.Keyboard, game);
var clockController = new ClockController(clock, app.Keyboard);

var paddle = game.CreatePaddle();
paddle.Spawn();

var ball = game.CreateBall();
ball.Spawn();

CreateAndSpawnBricks(game);

clock.Start();
while (!app.IsCloseRequested)
{
    app.Update();

    paddleController.Update();
    clockController.Update();

    if (app.Keyboard.WasKeyPressedThisFrame(KeyCode.Space))
    {
        var newBall = game.CreateBall();
        newBall.Spawn();
    }
    
    game.Update();
    app.Render(game);
    clock.Update();
}

return;

void CreateAndSpawnBricks(Game game)
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

IAppBuilder CreateAppBuilder()
{
    return new RaylibAppBuilder();
}