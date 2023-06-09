using EasyGameFramework.Api;
using Framework;

var builder = new GameBuilder();
builder.WithRenderer<TestRenderer>();

var app = builder.Build<SandboxGame>();
app.Run();
