namespace NodeGraphApp;

public static class App
{
    public static string ResolvePath(string path)
    {
        var appPath = AppContext.BaseDirectory;
        var fullFilePath = Path.Combine(appPath, path);
        return fullFilePath;
    }
}