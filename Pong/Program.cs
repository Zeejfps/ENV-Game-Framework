using EasyGameFramework.Api;
using Pong;

var builder = new GameBuilder();
var app = builder.Build<PongGame>();
app.Run();