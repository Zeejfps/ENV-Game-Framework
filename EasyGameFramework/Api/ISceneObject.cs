namespace EasyGameFramework.Api;

public interface ISceneObject
{
    void Load(IScene scene);
    void Update(float dt);
    void Unload(IScene scene);
    void Render();
}