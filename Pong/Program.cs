using EasyGameFramework.Builder;
using Pong;

var builder = new GameBuilder();
builder.With<ISpriteRenderer, SpriteRenderer>();
var game = builder.Build<PongGame>();
game.Run();