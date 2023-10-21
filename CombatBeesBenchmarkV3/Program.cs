using CombatBeesBenchmarkV3;
using EasyGameFramework.Builder;

var builder = new GameBuilder();
var game = builder.Build<CombatBeesBenchmarkGame>();
game.Launch();