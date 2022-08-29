using EasyGameFramework.API;
using EasyGameFramework.API.InputDevices;
using SnakeGame;

var builder = new ApplicationBuilder();
var app = builder.Build();

var window = app.Window;
window.Width = 500;
window.Height = 500;
window.IsVsyncEnabled = true;
window.IsResizable = false;
window.ShowCentered();

var game = new Game(app);

while (app.IsRunning)
{
    app.Update();

    if (app.Input.Keyboard.WasKeyPressedThisFrame(KeyboardKey.Escape))
    {
        app.Quit();
    }
    
    game.Update();
}

