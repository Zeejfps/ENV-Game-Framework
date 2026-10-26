using ZGF.Gui.Immediate;
using ZGF.ILSpy.Core;

namespace ZGF.ILSpy;

public sealed class CommandLine
{
    private const string SettingsOption = "--settings=";
    private const string OpenGlFlag = "--gl";
    private const string LanguageOption = "--language=";
    private const string ListOption = "--list=";
    private const string ClearListFlag = "--clear-list";
    private const string FlagPrefix = "--";

    public string SettingsPath { get; private set; } = AppSettings.DefaultPath;
    public RenderBackendKind RenderBackend { get; private set; } = RenderBackendKind.Auto;
    public List<string> AssemblyPaths { get; } = new();

    /// <summary>The language to decompile to, or null to keep the one the settings have.</summary>
    public DecompilationLanguage? Language { get; private set; }

    /// <summary>The name of the assembly list to open, made when there is none by that name, or null
    /// to open the one the settings have open.</summary>
    public string? AssemblyList { get; private set; }

    /// <summary>Whether to take every assembly off the list before opening <see cref="AssemblyPaths"/>.</summary>
    public bool ClearList { get; private set; }

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
        else if (arg.StartsWith(LanguageOption, StringComparison.Ordinal))
            Language = ParseLanguage(arg.Substring(LanguageOption.Length));
        else if (arg.StartsWith(ListOption, StringComparison.Ordinal))
            AssemblyList = ReadListName(arg.Substring(ListOption.Length));
        else if (arg == ClearListFlag)
            ClearList = true;
        else if (!arg.StartsWith(FlagPrefix, StringComparison.Ordinal))
            AssemblyPaths.Add(arg);
    }

    /// <summary>The language named <paramref name="name"/>, in any case; null for one the app does not have.</summary>
    private static DecompilationLanguage? ParseLanguage(string name)
    {
        var upper = name.ToUpperInvariant();
        return upper switch
        {
            "C#" => DecompilationLanguage.CSharp,
            "CSHARP" => DecompilationLanguage.CSharp,
            "IL" => DecompilationLanguage.IL,
            "IL WITH C#" => DecompilationLanguage.ILWithCSharp,
            "ILWITHCSHARP" => DecompilationLanguage.ILWithCSharp,
            _ => null,
        };
    }

    /// <summary>The list name without the spaces around it; null for a blank one.</summary>
    private static string? ReadListName(string name)
    {
        var trimmed = name.Trim();
        if (trimmed.Length == 0)
            return null;
        return trimmed;
    }
}
