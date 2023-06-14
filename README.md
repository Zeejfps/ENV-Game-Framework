# Getting Started

### MyGame.cs
```C#
using EasyGameFramework.Api;

public sealed class MyGame : Game
{
    public MyGame(IContext context) : base(context)
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

### Program.cs

```C#
using EasyGameFramework.Builder;

var builder = new GameBuilder();
var game = builder.Build<MyGame>();
game.Launch();
```

# Rendering