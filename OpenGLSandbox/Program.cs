// See https://aka.ms/new-console-template for more information

using EasyGameFramework.Api;
using EasyGameFramework.Api.AssetTypes;
using EasyGameFramework.Builder;
using EasyGameFramework.Core.AssetLoaders;
using OpenGLSandbox;

var builder = new GameBuilder();
builder.With<IAssetLoader<ICpuTexture>, CpuTextureAssetLoader>();

var game = builder.Build<OpenGlSandboxGame>();
game.Launch();