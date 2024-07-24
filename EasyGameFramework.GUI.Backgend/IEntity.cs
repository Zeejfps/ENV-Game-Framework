namespace OpenGLSandbox;

public interface IEntity<TComponent> where TComponent : unmanaged
{
    event Action<IEntity<TComponent>> BecameDirty;

    void LoadComponent(ref TComponent component);
}