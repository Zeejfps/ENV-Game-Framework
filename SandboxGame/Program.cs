using EasyGameFramework.API;
using Framework;

var builder = new ApplicationBuilder();

var app = builder.Build();

var game = new Game(app);
game.Run();
