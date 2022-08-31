using EasyGameFramework.Api;

namespace EasyGameFramework.Core;

internal class Engine : IEngine
{
    public Engine(IApp app)
    {
        App = app;
    }

    private IApp App { get; }

    public void Run()
    {
        var app = App;

        app.Setup();
        while (app.IsRunning)
        {
            app.Update();
        }
    }
}