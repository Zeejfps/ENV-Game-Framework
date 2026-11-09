namespace ZGF.AppUtils.Tests;

public sealed class LinuxDesktopEntryTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "zgf-desktop-" + Guid.NewGuid().ToString("N"));
    private readonly LinuxDesktopEntry _entry;

    private string DataHome => Path.Combine(_root, "share");
    private string DesktopPath => Path.Combine(DataHome, "applications", _entry.DesktopFileName);
    private string IconPath => Path.Combine(DataHome, "icons", _entry.IconFileName);
    private string IconSource => Path.Combine(_root, "app_icon.png");

    public LinuxDesktopEntryTests()
    {
        _entry = new LinuxDesktopEntry(
            fileStem: "DiffDino",
            name: "DiffDino",
            comment: "A desktop Git client",
            startupWmClass: "DiffDino",
            categories: "Development;RevisionControl;",
            iconSource: IconSource);
        Directory.CreateDirectory(_root);
        File.WriteAllText(IconSource, "png");
    }

    public void Dispose()
    {
        Directory.Delete(_root, recursive: true);
    }

    private static string ExecLine(string contents) =>
        contents.Split('\n').Single(line => line.StartsWith("Exec="));

    [Fact]
    public void FirstLaunchWritesTheEntryAndIcon()
    {
        Assert.True(_entry.Ensure("/home/me/.local/bin/DiffDino.AppImage", DataHome));

        var contents = File.ReadAllText(DesktopPath);
        Assert.Equal(_entry.Build("/home/me/.local/bin/DiffDino.AppImage", IconPath), contents);
        Assert.Contains("Exec=\"/home/me/.local/bin/DiffDino.AppImage\"\n", contents);
        Assert.Contains("TryExec=/home/me/.local/bin/DiffDino.AppImage\n", contents);
        Assert.Contains($"Icon={IconPath.Replace(@"\", @"\\")}\n", contents);
        Assert.Equal("png", File.ReadAllText(IconPath));
    }

    [Fact]
    public void AnUpToDateEntryIsLeftAlone()
    {
        _entry.Ensure("/opt/DiffDino.AppImage", DataHome);

        Assert.False(_entry.Ensure("/opt/DiffDino.AppImage", DataHome));
    }

    [Fact]
    public void AMovedAppImageRepointsTheEntry()
    {
        _entry.Ensure("/old/DiffDino.AppImage", DataHome);

        Assert.True(_entry.Ensure("/new/DiffDino.AppImage", DataHome));
        Assert.Equal("Exec=\"/new/DiffDino.AppImage\"", ExecLine(File.ReadAllText(DesktopPath)));
    }

    [Fact]
    public void AnEntryTheUserWroteIsNeverOverwritten()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(DesktopPath)!);
        const string mine = "[Desktop Entry]\nType=Application\nName=My DiffDino\nExec=/somewhere/else\n";
        File.WriteAllText(DesktopPath, mine);

        Assert.False(_entry.Ensure("/opt/DiffDino.AppImage", DataHome));
        Assert.Equal(mine, File.ReadAllText(DesktopPath));
    }

    [Fact]
    public void AnExistingIconIsKept()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(IconPath)!);
        File.WriteAllText(IconPath, "custom");

        _entry.Ensure("/opt/DiffDino.AppImage", DataHome);

        Assert.Equal("custom", File.ReadAllText(IconPath));
    }

    [Fact]
    public void ExecQuotesAndEscapesThePath()
    {
        var contents = _entry.Build("/a b/$x\"y`z\\w 100%/D.AppImage", "/i.png");

        Assert.Equal(@"Exec=""/a b/\\$x\\""y\\`z\\\\w 100%%/D.AppImage""", ExecLine(contents));
        Assert.Contains(@"TryExec=/a b/$x""y`z\\w 100%/D.AppImage" + "\n", contents);
    }

    [Fact]
    public void TheEntryMatchesWhatInstallShWrites()
    {
        var contents = _entry.Build(
            "/home/me/.local/bin/DiffDino.AppImage",
            "/home/me/.local/share/icons/DiffDino.png");

        Assert.Equal(
            "[Desktop Entry]\n" +
            "Type=Application\n" +
            "Name=DiffDino\n" +
            "Comment=A desktop Git client\n" +
            "Exec=\"/home/me/.local/bin/DiffDino.AppImage\"\n" +
            "TryExec=/home/me/.local/bin/DiffDino.AppImage\n" +
            "Icon=/home/me/.local/share/icons/DiffDino.png\n" +
            "StartupWMClass=DiffDino\n" +
            "Categories=Development;RevisionControl;\n" +
            "Terminal=false\n" +
            "X-DiffDino-Managed=true\n",
            contents);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Diff Dino")]
    [InlineData("a/b")]
    [InlineData("a\\b")]
    public void AnUnusableFileStemIsRejected(string stem)
    {
        Assert.Throws<ArgumentException>(() => new LinuxDesktopEntry(
            stem, "DiffDino", "A desktop Git client", "DiffDino", "Development;", IconSource));
    }

    [Fact]
    public void AMultiLineValueIsRejected()
    {
        Assert.Throws<ArgumentException>(() => new LinuxDesktopEntry(
            "DiffDino", "DiffDino", "A desktop\nGit client", "DiffDino", "Development;", IconSource));
    }
}
