using Bricks;
using EasyGameFramework.Builder;

var builder = new GameBuilder();
builder.With<ISpriteRenderer, OpenGlSpriteRenderer>();
var game = builder.Build<BricksGame>();
game.Launch();