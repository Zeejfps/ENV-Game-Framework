using EasyGameFramework.Api;
using Core;

var builder = new EngineBuilder();
builder.WithApp<SnakeGame>();

var engine = builder.Build();
engine.Run();