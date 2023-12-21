using EasyGameFramework.Builder;
using Tetris;

var builder = new GameBuilder();
var game = builder.Build<TetrisGame>();
game.Launch();