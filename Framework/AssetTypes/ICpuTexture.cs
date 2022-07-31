namespace Framework;

public interface ICpuTexture : ICpuAsset
{
    int Width { get; }
    int Height { get; }
    byte[] Pixels { get; }
}