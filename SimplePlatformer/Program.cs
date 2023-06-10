using EasyGameFramework.Api;
using EasyGameFramework.Builder;
using SimplePlatformer;

var builder = new GameBuilder();
var app = builder.Build<SimplePlatformerGame>();
app.Run();