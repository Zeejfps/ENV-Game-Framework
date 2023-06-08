using EasyGameFramework.Api;
using Framework;

var builder = new ApplicationBuilder();
builder.WithRenderer<TestRenderer>();

var app = builder.Build<SandboxGameApp>();
app.Run();
