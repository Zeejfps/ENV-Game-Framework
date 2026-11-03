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
    private const string NavigateToOption = "--navigateto=";
    private const string NewInstanceFlag = "--newinstance";
    private const string FlagPrefix = "--";

    public string SettingsPath { get; private set; } = AppSettings.DefaultPath;
    public RenderBackendKind RenderBackend { get; private set; } = RenderBackendKind.Auto;
    public List<AssemblyPath> AssemblyPaths { get; } = new();

    /// <summary>The language to decompile to, or null to keep the one the settings have.</summary>
    public DecompilationLanguage? Language { get; private set; }

    /// <summary>The name of the assembly list to open, made when there is none by that name, or null
    /// to open the one the settings have open.</summary>
    public string? AssemblyList { get; private set; }

    /// <summary>Whether to take every assembly off the list before opening <see cref="AssemblyPaths"/>.</summary>
    public bool ClearList { get; private set; }

    /// <summary>The documentation id of the namespace, type or member to show, such as
    /// <c>T:System.String</c>, looked for in <see cref="AssemblyPaths"/>, or in every open assembly when
    /// there are none; null to show the last file opened.</summary>
    public string? NavigateTo { get; private set; }

    /// <summary>Whether to open a window of its own rather than hand this command line to the one
    /// already open with the same settings.</summary>
    public bool NewInstance { get; private set; }

    /// <summary>The arguments that ask for nothing the app can do, as they were given: an option it does
    /// not have, a language it does not have, an option with no value, or a file that is no path.</summary>
    public List<string> Ignored { get; } = new();

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
            ReadSettingsPath(arg);
        else if (arg == OpenGlFlag)
            RenderBackend = RenderBackendKind.OpenGl;
        else if (arg.StartsWith(LanguageOption, StringComparison.Ordinal))
            ReadLanguage(arg);
        else if (arg.StartsWith(ListOption, StringComparison.Ordinal))
            ReadAssemblyList(arg);
        else if (arg == ClearListFlag)
            ClearList = true;
        else if (arg.StartsWith(NavigateToOption, StringComparison.Ordinal))
            ReadNavigateTo(arg);
        else if (arg == NewInstanceFlag)
            NewInstance = true;
        else if (arg.StartsWith(FlagPrefix, StringComparison.Ordinal))
            Ignored.Add(arg);
        else
            AddAssemblyPath(arg);
    }

    private void ReadSettingsPath(string arg)
    {
        var path = ReadName(arg.Substring(SettingsOption.Length));
        if (path == null)
        {
            Ignored.Add(arg);
            return;
        }
        SettingsPath = path;
    }

    private void ReadLanguage(string arg)
    {
        var language = ParseLanguage(arg.Substring(LanguageOption.Length));
        if (language == null)
        {
            Ignored.Add(arg);
            return;
        }
        Language = language;
    }

    private void ReadAssemblyList(string arg)
    {
        var name = ReadName(arg.Substring(ListOption.Length));
        if (name == null)
        {
            Ignored.Add(arg);
            return;
        }
        AssemblyList = name;
    }

    private void ReadNavigateTo(string arg)
    {
        var id = ReadName(arg.Substring(NavigateToOption.Length));
        if (id == null)
        {
            Ignored.Add(arg);
            return;
        }
        NavigateTo = id;
    }

    /// <summary>Takes <paramref name="arg"/> as a file, made absolute from this process's folder.</summary>
    private void AddAssemblyPath(string arg)
    {
        var path = AssemblyPath.Parse(Path.Combine(Environment.CurrentDirectory, arg));
        if (path == null)
        {
            Ignored.Add(arg);
            return;
        }
        AssemblyPaths.Add(path);
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

    /// <summary>The name without the spaces around it; null for a blank one.</summary>
    private static string? ReadName(string name)
    {
        var trimmed = name.Trim();
        if (trimmed.Length == 0)
            return null;
        return trimmed;
    }

    /// <summary>The arguments that ask a window already open for what this command line asks: the
    /// language, the list, what to show and the files, made absolute from this process's folder; and the
    /// arguments ignored, for that window to tell of.</summary>
    public List<string> ListHandedArguments()
    {
        var args = new List<string>();
        if (Language != null)
            args.Add(LanguageOption + Language.Value);
        if (AssemblyList != null)
            args.Add(ListOption + AssemblyList);
        if (ClearList)
            args.Add(ClearListFlag);
        if (NavigateTo != null)
            args.Add(NavigateToOption + NavigateTo);
        foreach (var path in AssemblyPaths)
            args.Add(path.Text);
        args.AddRange(Ignored);
        return args;
    }
}
