namespace EasyGameFramework.Api;

public interface IAllocator
{
    T New<T>();
}