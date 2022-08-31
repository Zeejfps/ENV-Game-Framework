using EasyGameFramework.Api;
using Framework;

var builder = new EngineBuilder();
builder.WithOpenGl();
builder.WithRenderer<TestRenderer>();
builder.WithApp<SandboxGame>();

var engine = builder.Build();
engine.Run();
