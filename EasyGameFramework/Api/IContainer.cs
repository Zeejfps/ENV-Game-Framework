namespace EasyGameFramework.Api;

public interface IContainer
{
    T New<T>();
    void BindFactory<T>(Func<object> factory);
    void BindSingleton<T>(T instance);
    void BindSingleton<TInterface, TConcrete>() where TConcrete : TInterface;
}