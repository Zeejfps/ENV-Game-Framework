namespace EasyGameFramework.API.AssetTypes;

public interface ICpuShader : IAsset
{
    string VertexShader { get; set; }
    string FragmentShader { get; set; }
}