namespace ENV.Engine;

public interface IFramebuffer
{
    int Width { get; }
    int Height { get; }
    void Clear();
    void Resize(int width, int height);
    void RenderMesh(IMesh mesh, IMaterial material);
    void RenderMesh(IMesh mesh, IMaterial material, ITexture texture);
}