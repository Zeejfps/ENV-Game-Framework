using EasyGameFramework.Builder;
using OOPEcs;

var builder = new GameBuilder();
var game = builder.Build<TestGame>();
game.Launch();