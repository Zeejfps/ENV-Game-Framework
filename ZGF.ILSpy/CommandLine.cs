using ZGF.Gui.Immediate;

namespace ZGF.ILSpy;

public sealed class CommandLine
{
    private const string SettingsOption = "--settings=";
    private const string OpenGlFlag = "--gl";
    private const string FlagPrefix = "--";

    public string SettingsPath { get; private set; } = AppSettings.DefaultPath;
    public RenderBackendKind RenderBackend { get; private set; } = RenderBackendKind.Auto;
    public List<string> AssemblyPaths { get; } = new();

    public static CommandLine Parse(string[] args)
    {
        var commandLine = new CommandLine();
        foreach (var arg in args)
            commandLine.Read(arg);
        return commandLine;
    }

    private void Read(string arg)
    {
        if (arg.StartsWith(SettingsOption, StringComparison.Ordinal))
            SettingsPath = arg.Substring(SettingsOption.Length);
        else if (arg == OpenGlFlag)
            RenderBackend = RenderBackendKind.OpenGl;
        else if (!arg.StartsWith(FlagPrefix, StringComparison.Ordinal))
            AssemblyPaths.Add(arg);
    }
}
