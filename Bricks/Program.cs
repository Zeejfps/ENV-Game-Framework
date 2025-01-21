using Bricks;
using Bricks.RaylibBackend;

using var engine = CreateEngineBuilder()
    .WithWindowName("Brickz")
    .WithFramebufferSize(640, 480)
    .Build();

var game = new BrickzGame(engine);
engine.Run(game);
return;

IEngineBuilder CreateEngineBuilder()
{
    return new RaylibEngineBuilder();
}

