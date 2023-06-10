using EasyGameFramework.Builder;
using Pong;

var builder = new GameBuilder();
var game = builder.Build<PongGame>();
game.Run();