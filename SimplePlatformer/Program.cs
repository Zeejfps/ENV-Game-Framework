using EasyGameFramework.Api;
using SimplePlatformer;

var builder = new GameBuilder();
var app = builder.Build<SimplePlatformerGame>();
app.Run();