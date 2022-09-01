namespace EasyGameFramework.Api;

public interface IContainer
{
    T New<T>();
    void Register<T>(Func<object> factory);
}