namespace EasyGameFramework.API;

public interface IRenderer
{
    void Add(IRenderable renderable);

    void Remove(IRenderable renderable);
    
    void Render(IGpu gpu, ICamera camera);
}