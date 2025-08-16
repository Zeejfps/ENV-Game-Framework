namespace ZGF.AppUtils;

public static class PathUtils
{
    public static string ResolveLocalPath(string path)
    {
        var appPath = AppContext.BaseDirectory;
        var fullFilePath = Path.Combine(appPath, path);
        return fullFilePath;
    }
}