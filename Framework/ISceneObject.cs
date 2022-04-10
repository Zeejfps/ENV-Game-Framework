using ENV.Engine;

namespace ENV;

public interface ISceneObject
{
    void Load(IScene scene);
    void Update(IScene scene);
    void Unload(IScene scene);
}