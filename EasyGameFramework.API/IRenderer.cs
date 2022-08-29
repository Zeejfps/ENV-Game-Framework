namespace EasyGameFramework.API;

public interface IRenderer
{
    void Render(IGpu gpu, ICamera camera, IRenderScene renderScene);
}