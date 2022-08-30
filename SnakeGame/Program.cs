using EasyGameFramework;
using EasyGameFramework.API;
using Snake;

var builder = new EngineBuilder();
builder.WithGame<SnakeGame>();

var engine = builder.Build();
engine.Run();