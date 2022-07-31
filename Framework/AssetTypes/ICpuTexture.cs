namespace Framework;

public interface ICpuTexture : IAsset
{
    int Width { get; }
    int Height { get; }
    byte[] Pixels { get; }
}