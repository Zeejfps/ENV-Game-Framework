using Bricks;
using Bricks.RaylibBackend;


var appBuilder = CreateAppBuilder();
appBuilder.WithWindowName("Brickz");
appBuilder.WithCanvasSize(640, 480);
using var app = appBuilder.Build();

var clock = new StopwatchClock();
var arena = Rectangle.LeftTopWidthHeight(0, 0, 640, 480);
var paddle = new Paddle(app.Input, clock, arena);

clock.Start();
while (!app.IsCloseRequested)
{
    app.Update();
    paddle.Update();
    app.Render(paddle);
    clock.Update();
}


IAppBuilder CreateAppBuilder()
{
    return new RaylibAppBuilder();
}