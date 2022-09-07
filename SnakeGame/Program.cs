using SampleGames;
using EasyGameFramework.Api;

var builder = new ApplicationBuilder();

var app = builder.Build<SnakeGameApp>();
app.Run();