using Bricks;
using EasyGameFramework.Api;
using EasyGameFramework.Api.AssetTypes;
using EasyGameFramework.Builder;
using EasyGameFramework.Core.AssetLoaders;

var builder = new GameBuilder();
builder.With<ISpriteRenderer, OpenGlSpriteRenderer>();
builder.With<IAssetLoader<ICpuTexture>, CpuTextureAssetLoader>();
var game = builder.Build<BricksGame>();
game.Launch();