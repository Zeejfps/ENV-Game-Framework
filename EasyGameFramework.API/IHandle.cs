namespace EasyGameFramework.API;

public interface IHandle<T> where T : IDisposable
{
    T Use();
}