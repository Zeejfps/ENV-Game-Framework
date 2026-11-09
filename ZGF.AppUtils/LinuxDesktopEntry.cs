using System.Text;

namespace ZGF.AppUtils;

public sealed class LinuxDesktopEntry
{
    private readonly string _name;
    private readonly string _comment;
    private readonly string _startupWmClass;
    private readonly string _categories;
    private readonly string _iconSource;

    public string DesktopFileName { get; }
    public string IconFileName { get; }
    public string ManagedMarker { get; }

    public LinuxDesktopEntry(
        string fileStem,
        string name,
        string comment,
        string startupWmClass,
        string categories,
        string iconSource)
    {
        if (string.IsNullOrEmpty(fileStem)
            || fileStem.Any(char.IsWhiteSpace)
            || fileStem.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
            || fileStem.IndexOfAny(['/', '\\']) >= 0)
            throw new ArgumentException($"Not a usable file stem: '{fileStem}'", nameof(fileStem));

        _name = SingleLine(name, nameof(name));
        _comment = SingleLine(comment, nameof(comment));
        _startupWmClass = SingleLine(startupWmClass, nameof(startupWmClass));
        _categories = SingleLine(categories, nameof(categories));
        _iconSource = iconSource;

        DesktopFileName = fileStem + ".desktop";
        IconFileName = fileStem + ".png";
        ManagedMarker = $"X-{fileStem}-Managed=true";
    }

    public void EnsureRegistered()
    {
        if (!OperatingSystem.IsLinux()) return;

        var appImage = Environment.GetEnvironmentVariable("APPIMAGE");
        if (string.IsNullOrEmpty(appImage) || !Path.IsPathRooted(appImage)) return;

        try
        {
            Ensure(appImage, DataHome());
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"Could not update the application menu entry: {e.Message}");
        }
    }

    public bool Ensure(string appImagePath, string dataHome)
    {
        var iconDirectory = Path.Combine(dataHome, "icons");
        var iconPath = Path.Combine(iconDirectory, IconFileName);
        if (!File.Exists(iconPath) && File.Exists(_iconSource))
        {
            Directory.CreateDirectory(iconDirectory);
            File.Copy(_iconSource, iconPath, overwrite: true);
        }

        var applicationsDirectory = Path.Combine(dataHome, "applications");
        var desktopPath = Path.Combine(applicationsDirectory, DesktopFileName);
        var contents = Build(appImagePath, iconPath);
        if (File.Exists(desktopPath))
        {
            var existing = File.ReadAllText(desktopPath);
            if (existing == contents) return false;
            if (!existing.Split('\n').Any(line => line.Trim() == ManagedMarker)) return false;
        }

        Directory.CreateDirectory(applicationsDirectory);
        var part = desktopPath + ".part";
        File.WriteAllText(part, contents);
        File.Move(part, desktopPath, overwrite: true);
        return true;
    }

    public string Build(string appImagePath, string iconPath) => string.Join('\n',
    [
        "[Desktop Entry]",
        "Type=Application",
        "Name=" + _name,
        "Comment=" + _comment,
        "Exec=" + ExecValue(appImagePath),
        "TryExec=" + StringValue(appImagePath),
        "Icon=" + StringValue(iconPath),
        "StartupWMClass=" + _startupWmClass,
        "Categories=" + _categories,
        "Terminal=false",
        ManagedMarker,
        "",
    ]);

    private static string SingleLine(string value, string parameterName)
    {
        if (string.IsNullOrEmpty(value) || value.IndexOfAny(['\n', '\r']) >= 0)
            throw new ArgumentException($"Must be a non-empty single line: '{value}'", parameterName);
        return value;
    }

    // Desktop Entry spec, "Possible value types": string values escape backslashes; Exec also
    // quotes the path, backslash-escapes \ " ` $ inside the quotes, and doubles a literal %.
    private static string StringValue(string value) => value.Replace(@"\", @"\\");

    private static string ExecValue(string path)
    {
        var quoted = new StringBuilder(path.Length + 8);
        foreach (var c in path)
        {
            if (c is '\\' or '"' or '`' or '$') quoted.Append('\\');
            if (c == '%') quoted.Append('%');
            quoted.Append(c);
        }
        return StringValue("\"" + quoted + "\"");
    }

    private static string DataHome()
    {
        var xdg = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
        if (!string.IsNullOrEmpty(xdg) && Path.IsPathRooted(xdg)) return xdg;
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share");
    }
}
