// See https://aka.ms/new-console-template for more information

using EasyGameFramework.Builder;
using OpenGLSandbox;

var game = new GameBuilder().Build<OpenGlSandboxGame>();
game.Launch();