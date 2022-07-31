namespace Framework;

public interface ICpuShader : ICpuAsset
{
    string VertexShader { get; set; }
    string FragmentShader { get; set; }
}