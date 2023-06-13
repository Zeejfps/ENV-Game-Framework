using EasyGameFramework.Builder;

var builder = new GameBuilder();
var app = builder.Build<SnakeGame>();
app.Launch();