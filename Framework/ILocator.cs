namespace Framework;

public interface ILocator
{
    T? Locate<T>();
    T LocateOrThrow<T>();

    void RegisterSingleton<T>(T singleton);
}