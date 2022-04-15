// See https://aka.ms/new-console-template for more information

using GlfwOpenGLBackend;
using SnakeGame;

using var context = new Context_GLFW();

var primaryDisplay = context.Displays.PrimaryDisplay;
var window = context.Window;

window.Width = 500;
window.Height = 500;
window.PosX = (int)((primaryDisplay.ResolutionX - window.Width) * 0.5f);
window.PosY = (int)((primaryDisplay.ResolutionY - window.Height) * 0.5f);
window.IsVsyncEnabled = true;
window.IsResizable = false;
window.Open();

var game = new Game(context);

while (window.IsOpened)
{
    window.Update();
    game.Update();
}

