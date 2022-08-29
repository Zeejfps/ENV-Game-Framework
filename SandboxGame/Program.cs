using EasyGameFramework.API;
using Framework;

var builder = new ApplicationBuilder();
builder.WithGlfwOpenGlBackend();
builder.WithRenderer<ScriptableRenderer>();

var app = builder.Build();

var game = new Game(app);
game.Run();
