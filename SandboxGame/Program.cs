using EasyGameFramework.API;
using Framework;

var builder = new EngineBuilder();
builder.WithGlfwOpenGlBackend();
builder.WithRenderer<TestRenderer>();
builder.WithGame<SandboxGame>();

var engine = builder.Build();
engine.Run();
