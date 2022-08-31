using EasyGameFramework.Api;
using Core;

var builder = new EngineBuilder();
builder.WithGame<SnakeGame>();

var engine = builder.Build();
engine.Run();