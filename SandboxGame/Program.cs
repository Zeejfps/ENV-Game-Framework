using EasyGameFramework;
using EasyGameFramework.Api;
using Framework;

var builder = new EngineBuilder();
builder.WithGlfw();
builder.WithOpenGl();
builder.WithRenderer<TestRenderer>();
builder.WithGame<SandboxGame>();

var engine = builder.Build();
engine.Run();
