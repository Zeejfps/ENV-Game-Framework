using EasyGameFramework.API;
using TicTacToePrototype;

namespace Framework;

public class Game
{
    IDisplays Displays => App.Displays;
    IWindow Window => App.Window;
    IApplication App { get; }

    public Game(IApplication app)
    {
        App = app;
    }
    
    public void Run()
    {
        Window.Title = "Hello World";
        //Window.IsFullscreen = true;
        Window.Width = 1280;
        Window.Height = 720;
        Window.IsResizable = true;
        Window.IsVsyncEnabled = true;
        Window.ShowCentered();
        
        var scene = new TestScene(App);
        scene.Load();
        
        while (App.IsRunning)
        {
            App.Update();
            scene.Update();
        }
    }
}