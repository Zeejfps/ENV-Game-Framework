using Framework;

namespace Framework;

public interface ISceneObject
{
    void Load(IScene scene);
    void Update(IScene scene);
    void Unload(IScene scene);
}