namespace ZGF.Core;

public static class AppHost
{
    public static IAppHostBuilder Builder()
    {
        return null;
    }
}

public interface IAppHostBuilder
{
    IAppHost Build();
}

public interface IAppHost
{
    
}