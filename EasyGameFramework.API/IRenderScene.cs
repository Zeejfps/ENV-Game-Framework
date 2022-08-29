namespace EasyGameFramework.API;

public interface IRenderScene
{
    void Add(IRenderable renderable);

    void Remove(IRenderable renderable);
}