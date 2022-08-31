using EasyGameFramework.Api;

namespace EasyGameFramework.Core;

internal class Engine : IEngine
{
    public Engine(IInput input, IWindow window, IApp app)
    {
        App = app;
        Input = input;
        Window = window;
    }

    private IApp App { get; }
    private IInput Input { get; }
    private IWindow Window { get; }

    public void Run()
    {
        var app = App;
        var input = Input;
        var window = Window;

        app.Start();
        while (app.IsRunning && window.IsOpened)
        {
            input.Update();
            window.Update();
            app.Update();
        }
    }
}