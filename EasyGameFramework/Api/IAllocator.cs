namespace EasyGameFramework.Api;

public interface IAllocator
{
    T New<T>();
    void Delete<T>(T obj);
}