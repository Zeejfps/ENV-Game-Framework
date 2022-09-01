namespace EasyGameFramework.Api;

public interface IContainer
{
    T New<T>();
    void BindFactory<T>(Func<object> factory);
    void BindInstance<T>(T instance);
}