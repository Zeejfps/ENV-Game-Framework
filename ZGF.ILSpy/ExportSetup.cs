using ZGF.Desktop;
using ZGF.Gui.Immediate.Widgets;
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

/// <summary>What keeps an export from starting.</summary>
public enum ExportBlocker
{
    None,
    NoAssembly,
    NoFolder,
    NamelessProject,
    Clash,
    KeyFileMissing,
}

/// <summary>The export dialog's pages: what is exported and how, or which assemblies.</summary>
public enum ExportPage
{
    Main,
    Choose,
}

/// <summary>
/// What the export dialog asks for: which open assemblies to export, under which project names,
/// into which folder, and for several, the solution's name; besides, a strong-name key to sign
/// with and whether to write a PDB beside each project. One assembly exports as a project
/// straight into the folder, several as a solution with a folder per project. Project names start
/// out unique; only renaming can make two clash, which stops the export until it is fixed, as a
/// key file that is not there does. Everything but the ticks stays for the next time. The
/// assemblies are ticked on a page of their own, which the dialog opens on when none is ticked yet.
/// </summary>
public sealed class ExportSetup
{
    private const string DefaultSolutionName = "Solution";

    private string? _checkedFolder;
    private bool _folderHoldsFiles;
    private string? _checkedKeyFile;
    private bool _keyFileMissing;

    public bool IsOpen;
    public string Folder = "";
    public string SolutionName = "";

    /// <summary>The strong-name key file to copy into each project and sign it with; none when empty.</summary>
    public string StrongNameKeyFile = "";

    /// <summary>Whether a portable PDB is written beside each project.</summary>
    public bool GeneratePdb;

    /// <summary>Whether those PDBs embed the decompiled sources.</summary>
    public bool EmbedSourcesInPdb = true;

    /// <summary>Where the dialog's window was left, on the screen; null opens it centered.</summary>
    public WindowBounds? Window;

    public List<ExportChoice> Choices { get; } = new();

    public ExportPage Page;

    public AssemblyPicker Picker { get; } = new();

    /// <summary>The rows of the ticked assemblies picked out on the main page, to remove together.</summary>
    public MultiSelection<ExportChoice> RowSelection { get; } = new();

    /// <summary>Opens the dialog on <paramref name="assemblies"/> with <paramref name="picked"/>
    /// ticked, or the only one when there is just one. The folder stays as it was last time.</summary>
    public void Open(IReadOnlyList<LoadedAssembly> assemblies, LoadedAssembly? picked)
    {
        Choices.Clear();
        foreach (var project in AssemblyExport.NameProjects(assemblies))
            Choices.Add(new ExportChoice(project.Assembly, project.Name));
        TickFirstChoice(picked);
        RowSelection.Clear();
        SolutionName = "";
        IsOpen = true;
        Page = ExportPage.Main;
        if (IncludedCount == 0)
            Choose();
    }

    private void TickFirstChoice(LoadedAssembly? picked)
    {
        foreach (var choice in Choices)
            choice.Included = choice.Assembly == picked;
        if (Choices.Count == 1)
            Choices[0].Included = true;
    }

    public void Close() => IsOpen = false;

    /// <summary>Shows the Choose page, its list arranged afresh.</summary>
    public void Choose()
    {
        Picker.Arrange(Choices);
        Page = ExportPage.Choose;
    }

    /// <summary>Goes back from the Choose page to what is exported.</summary>
    public void FinishChoosing()
    {
        Page = ExportPage.Main;
    }

    /// <summary>Unticks the assemblies whose rows are picked out, and picks out the row now where the
    /// first of them was.</summary>
    public void RemoveSelectedRows()
    {
        var order = IncludedChoices();
        var removed = RowSelection.InOrder(order);
        if (removed.Count == 0)
            return;
        var at = order.IndexOf(removed[0]);
        foreach (var choice in removed)
            choice.Included = false;
        RowSelection.SelectAt(at, IncludedChoices());
    }

    /// <summary>The ticked assemblies, in the order they were opened.</summary>
    public List<ExportChoice> IncludedChoices()
    {
        return Choices
            .Where(static c => c.Included)
            .ToList();
    }

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
    public string? Clash => ClashingChoice?.Name.Trim();

    /// <summary>The second of two ticked assemblies whose project names are the same, or null.</summary>
    public ExportChoice? ClashingChoice
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
                    return choice;
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

    /// <summary>Whether a key file is named that is not there. Looked up again only when the name changes.</summary>
    public bool KeyFileMissing
    {
        get
        {
            if (_checkedKeyFile == StrongNameKeyFile)
                return _keyFileMissing;
            _checkedKeyFile = StrongNameKeyFile;
            _keyFileMissing = IsMissing(StrongNameKeyFile.Trim());
            return _keyFileMissing;
        }
    }

    private static bool IsMissing(string file)
    {
        if (file.Length == 0)
            return false;
        return !File.Exists(file);
    }

    /// <summary>The first thing that keeps the export from starting, or <see cref="ExportBlocker.None"/>:
    /// it needs at least one assembly, a folder, a name for each project that no other takes, and no
    /// key file that is not there.</summary>
    public ExportBlocker Blocker
    {
        get
        {
            if (IncludedCount == 0)
                return ExportBlocker.NoAssembly;
            if (!HasFolder)
                return ExportBlocker.NoFolder;
            if (HasNamelessProject)
                return ExportBlocker.NamelessProject;
            if (Clash != null)
                return ExportBlocker.Clash;
            if (KeyFileMissing)
                return ExportBlocker.KeyFileMissing;
            return ExportBlocker.None;
        }
    }

    public bool CanExport => Blocker == ExportBlocker.None;

    /// <summary>What each project is written with besides its code.</summary>
    public ProjectExtras Extras()
    {
        var key = StrongNameKeyFile.Trim();
        var keyFile = key.Length == 0 ? null : key;
        return new ProjectExtras(keyFile, GeneratePdb, EmbedSourcesInPdb);
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
