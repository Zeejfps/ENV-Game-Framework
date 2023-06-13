using DataOriented.Pong;
using EasyGameFramework.Builder;

var builder = new GameBuilder();
var game = builder.Build<PongGame>();
game.Launch();