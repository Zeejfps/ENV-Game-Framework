using EasyGameFramework.API;
using SnakeGame;

var builder = new ApplicationBuilder();
var app = builder.Build();

var window = app.Window;
window.Width = 500;
window.Height = 500;
window.IsVsyncEnabled = true;
window.IsResizable = false;
window.OpenCentered();

var game = new Game(app);

while (app.IsRunning)
{
    app.Update();
    game.Update();
}

