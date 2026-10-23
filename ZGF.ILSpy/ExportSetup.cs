using ZGF.ILSpy.Core;

namespace ZGF.ILSpy;

/// <summary>An open assembly as the export dialog lists it: whether it is exported, and the name of
/// its project.</summary>
public sealed class ExportChoice(LoadedAssembly assembly, string name)
{
    public LoadedAssembly Assembly { get; } = assembly;
    public bool Included;
    public string Name = name;
}

/// <summary>
/// What the export dialog asks for: which open assemblies to export, under which project names,
/// into which folder, and for several, the solution's name. One assembly exports as a project
/// straight into the folder, several as a solution with a folder per project. Project names start
/// out unique; only renaming can make two clash, which stops the export until it is fixed.
/// </summary>
public sealed class ExportSetup
{
    private const string DefaultSolutionName = "Solution";

    private string? _checkedFolder;
    private bool _folderHoldsFiles;

    public bool IsOpen;
    public string Folder = "";
    public string SolutionName = "";

    public List<ExportChoice> Choices { get; } = new();

    /// <summary>Opens the dialog on <paramref name="assemblies"/> with <paramref name="picked"/>
    /// ticked, or the only one when there is just one. The folder stays as it was last time.</summary>
    public void Open(IReadOnlyList<LoadedAssembly> assemblies, LoadedAssembly? picked)
    {
        Choices.Clear();
        foreach (var project in AssemblyExport.NameProjects(assemblies))
            Choices.Add(new ExportChoice(project.Assembly, project.Name));
        TickFirstChoice(picked);
        SolutionName = "";
        IsOpen = true;
    }

    private void TickFirstChoice(LoadedAssembly? picked)
    {
        foreach (var choice in Choices)
            choice.Included = choice.Assembly == picked;
        if (Choices.Count == 1)
            Choices[0].Included = true;
    }

    public void Close() => IsOpen = false;

    public int IncludedCount => Choices.Count(static c => c.Included);

    /// <summary>Whether the export writes a solution: more than one assembly is ticked.</summary>
    public bool IsSolution => IncludedCount > 1;

    /// <summary>Ticks every assembly when <paramref name="all"/>, else none.</summary>
    public void IncludeAll(bool all)
    {
        foreach (var choice in Choices)
            choice.Included = all;
    }

    /// <summary>Whether every assembly is ticked, none is, or some are (null).</summary>
    public bool? AllIncluded
    {
        get
        {
            var count = IncludedCount;
            if (count == 0)
                return false;
            if (count == Choices.Count)
                return true;
            return null;
        }
    }

    /// <summary>The solution's file name: the typed name, else the folder's.</summary>
    public string SolutionFileName => AssemblyExport.SolutionFileName(SolutionNameOrDefault());

    private string SolutionNameOrDefault()
    {
        var typed = SolutionName.Trim();
        if (typed.Length > 0)
            return typed;
        return DefaultSolutionNameFor(Folder);
    }

    /// <summary>The name a solution in <paramref name="folder"/> gets when none is typed: the folder's.</summary>
    public static string DefaultSolutionNameFor(string folder)
    {
        var trimmed = folder.Trim().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var name = Path.GetFileName(trimmed);
        if (string.IsNullOrEmpty(name))
            return DefaultSolutionName;
        return name;
    }

    /// <summary>A project name two ticked assemblies share, which must change before exporting, or null.</summary>
    public string? Clash
    {
        get
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var choice in Choices)
            {
                if (!choice.Included)
                    continue;
                var name = AssemblyExport.CleanProjectName(choice.Name);
                if (!names.Add(name))
                    return choice.Name.Trim();
            }
            return null;
        }
    }

    private bool HasNamelessProject => Choices.Any(static c => c.Included && c.Name.Trim().Length == 0);

    /// <summary>Whether the folder is a full path.</summary>
    public bool HasFolder => Path.IsPathFullyQualified(Folder.Trim());

    /// <summary>Whether the folder already holds files, which the export may replace. Looked up
    /// again only when the folder changes.</summary>
    public bool FolderHoldsFiles
    {
        get
        {
            if (_checkedFolder == Folder)
                return _folderHoldsFiles;
            _checkedFolder = Folder;
            _folderHoldsFiles = HoldsFiles(Folder.Trim());
            return _folderHoldsFiles;
        }
    }

    private static bool HoldsFiles(string folder)
    {
        if (!Path.IsPathFullyQualified(folder))
            return false;
        if (!Directory.Exists(folder))
            return false;
        return Directory.EnumerateFileSystemEntries(folder).Any();
    }

    /// <summary>Whether the export can start: a folder, at least one assembly, and a name for each
    /// project that no other takes.</summary>
    public bool CanExport
    {
        get
        {
            if (!HasFolder)
                return false;
            if (IncludedCount == 0)
                return false;
            if (HasNamelessProject)
                return false;
            return Clash == null;
        }
    }

    /// <summary>The ticked assemblies as the projects to export, in the dialog's order.</summary>
    public List<SolutionProject> IncludedProjects()
    {
        return Choices
            .Where(static c => c.Included)
            .Select(static c => new SolutionProject(c.Assembly, c.Name.Trim()))
            .ToList();
    }
}
