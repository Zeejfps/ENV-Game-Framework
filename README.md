How to use

implement Game.cs
Example:

```C#
using EasyGameFramework.Api;

public sealed class PongGame : Game
{
    public PongGame(IContext context) : base(context)
    {
    }

    protected override void OnStartup()
    {
        var window = Window;
        window.Title = "Pong - Data Oriented";
        window.IsResizable = true;
        window.SetScreenSize(640, 640);
    }

    protected override void OnUpdate()
    {
    }

    protected override void OnRender()
    {
    }

    protected override void OnShutdown()
    {
    }
}
```

Program.cs
Create GameBuilder
Call Build method
Call Run method

```C#
var builder = new GameBuilder();
var game = builder.Build<PongGame>();
game.Launch();
```
