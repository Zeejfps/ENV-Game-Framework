using EasyGameFramework.Api;
using Pong;

var builder = new ApplicationBuilder();
var app = builder.Build<PongApp>();
app.Run();