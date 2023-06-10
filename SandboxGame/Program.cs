using EasyGameFramework.Builder;
using Framework;

var builder = new GameBuilder();
var app = builder.Build<SandboxGame>();
app.Run();
