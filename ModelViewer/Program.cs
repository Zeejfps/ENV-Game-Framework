using EasyGameFramework.Builder;
using ModelViewer;

var builder = new GameBuilder();
var app = builder.Build<ModelViewerApp>();
app.Launch();