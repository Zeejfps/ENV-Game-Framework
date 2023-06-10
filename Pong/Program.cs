using EasyGameFramework.Api;
using EasyGameFramework.Builder;
using Pong;

var builder = new GameBuilder();
var app = builder.Build<PongGame>();
app.Run();