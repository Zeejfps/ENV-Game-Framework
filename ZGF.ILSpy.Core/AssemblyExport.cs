using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp.ProjectDecompiler;
using ICSharpCode.Decompiler.DebugInfo;
using ICSharpCode.Decompiler.Solution;

namespace ZGF.ILSpy.Core;

/// <summary>Something the decompiler could not handle: what it was doing, and the full error.</summary>
public sealed record ExportError(string Message, string Details);

/// <summary>An assembly decompiled into a C# project: the project file, and what could not be
/// decompiled, which the project holds as error comments instead. When a PDB was asked for,
/// <see cref="PdbPath"/> is where it was written, or <see cref="PdbFailure"/> says why it was not.</summary>
public sealed record ExportedProject(LoadedAssembly Assembly, string ProjectPath, IReadOnlyList<ExportError> Errors)
{
    internal ProjectId? Id { get; init; }

    public string? PdbPath { get; init; }

    public PdbFailure? PdbFailure { get; init; }
}

/// <summary>What a project export writes besides the project: the strong-name key it copies into
/// the project and signs with, and a portable PDB beside the project, with the decompiled sources
/// embedded or not.</summary>
public sealed record ProjectExtras(string? StrongNameKeyFile = null, bool Pdb = false, bool EmbedSourcesInPdb = true)
{
    public static ProjectExtras None { get; } = new();
}

/// <summary>An assembly to export as a project of a solution, under <see cref="Name"/>, which is
/// also the name of the folder the project goes in.</summary>
public sealed record SolutionProject(LoadedAssembly Assembly, string Name);

/// <summary>An assembly no PDB was written for: <see cref="Error"/> says why, or is null when its
/// debug directory names no PDB for one to match.</summary>
public sealed record PdbFailure(LoadedAssembly Assembly, ExportError? Error);

/// <summary>How far an export has got: it works on part <see cref="Part"/> of <see cref="Parts"/>,
/// named <see cref="Name"/>, and has done <see cref="Fraction"/> of the whole export, from 0 to 1,
/// or an unknown share when null.</summary>
public readonly record struct ExportProgress(string Name, int Part, int Parts, float? Fraction);

/// <summary>Writing assemblies out as C# projects, solutions and portable PDBs, reporting how far
/// it has got and stopping when cancelled.</summary>
public static class AssemblyExport
{
    internal const string ProjectExtension = ".csproj";
    internal const string PdbExtension = ".pdb";

    /// <summary>Decompiles <paramref name="assembly"/> into a project whose file is
    /// <paramref name="projectPath"/>, with its code and resources in the folder beside it, and
    /// writes what <paramref name="extras"/> asks for besides.</summary>
    public static ExportedProject ExportProject(LoadedAssembly assembly, DecompilerOptions options, string projectPath, ProjectExtras? extras = null,
        IProgress<ExportProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        var part = Part(progress, assembly, 0, WholeExport);
        return ExportProject(assembly, options, projectPath, extras ?? ProjectExtras.None, part, cancellationToken);
    }

    /// <summary>Exports the project, then the PDB when one is asked for; each takes half of the
    /// part's share of the export.</summary>
    private static ExportedProject ExportProject(LoadedAssembly assembly, DecompilerOptions options, string projectPath, ProjectExtras extras,
        PartProgress? part, CancellationToken cancellationToken)
    {
        var (projectPart, pdbPart) = SplitForPdb(part, extras.Pdb);
        var project = WriteProject(assembly, options, projectPath, extras.StrongNameKeyFile, projectPart, cancellationToken);
        if (!extras.Pdb)
            return project;
        var folder = Path.GetDirectoryName(projectPath)!;
        return WithPdb(project, options, folder, extras.EmbedSourcesInPdb, pdbPart, cancellationToken);
    }

    private static (PartProgress? Project, PartProgress? Pdb) SplitForPdb(PartProgress? part, bool pdb)
    {
        if (part == null)
            return (null, null);
        if (!pdb)
            return (part, null);
        return part.Halves();
    }

    private static ExportedProject WriteProject(LoadedAssembly assembly, DecompilerOptions options, string projectPath, string? strongNameKeyFile,
        IProgress<DecompilationProgress>? progress, CancellationToken cancellationToken)
    {
        var folder = Path.GetDirectoryName(projectPath)!;
        Directory.CreateDirectory(folder);
        var decompiler = CreateProjectDecompiler(assembly, options, cancellationToken);
        decompiler.ProgressIndicator = progress;
        decompiler.StrongNameKeyFile = strongNameKeyFile;
        using var projectFile = new StreamWriter(projectPath);
        var id = decompiler.DecompileProject(assembly.File, folder, projectFile, cancellationToken);
        var errors = decompiler.Errors
            .Select(static e => new ExportError(e.Message, e.ToString()))
            .ToList();
        return new ExportedProject(assembly, projectPath, errors) { Id = id };
    }

    private static WholeProjectDecompiler CreateProjectDecompiler(LoadedAssembly assembly, DecompilerOptions options, CancellationToken cancellationToken)
    {
        var settings = Settings.Create(options);
        var symbols = NodeDecompiler.DebugSymbolsFor(assembly, settings);
        return new BamlProjectDecompiler(assembly, settings, symbols, cancellationToken);
    }

    /// <summary><paramref name="project"/> with a portable PDB of its assembly written beside it, or
    /// with why it could not be.</summary>
    private static ExportedProject WithPdb(ExportedProject project, DecompilerOptions options, string folder, bool embedSources,
        IProgress<DecompilationProgress>? progress, CancellationToken cancellationToken)
    {
        var failure = TryGeneratePdb(project.Assembly, options, folder, embedSources, progress, cancellationToken);
        if (failure != null)
            return project with { PdbFailure = failure };
        var path = Path.Combine(folder, project.Assembly.PdbFileName);
        return project with { PdbPath = path };
    }

    /// <summary>Decompiles each of <paramref name="projects"/> into a project in a folder of its own,
    /// named as the project, beside <paramref name="solutionPath"/>, then writes the solution that
    /// holds them.</summary>
    public static List<ExportedProject> ExportSolution(IReadOnlyList<SolutionProject> projects, DecompilerOptions options, string solutionPath,
        ProjectExtras? extras = null, IProgress<ExportProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        var withExtras = extras ?? ProjectExtras.None;
        var folder = Path.GetDirectoryName(solutionPath)!;
        var assemblies = projects.Select(static p => p.Assembly).ToList();
        var bounds = PartBounds(assemblies);
        var exported = new List<ExportedProject>();
        for (var i = 0; i < projects.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var project = projects[i];
            var projectPath = ProjectPathIn(folder, project.Name);
            var part = Part(progress, project.Assembly, i, bounds);
            exported.Add(ExportProject(project.Assembly, options, projectPath, withExtras, part, cancellationToken));
        }
        var items = exported.ConvertAll(ToSolutionItem);
        SolutionCreator.WriteSolutionFile(solutionPath, items);
        return exported;
    }

    /// <summary>Where the project named <paramref name="name"/> of a solution in
    /// <paramref name="folder"/> is written: in a folder of its own, named as it is.</summary>
    public static string ProjectPathIn(string folder, string name)
    {
        var clean = CleanProjectName(name);
        return Path.Combine(folder, clean, clean + ProjectExtension);
    }

    /// <summary>The file name of a solution named <paramref name="name"/>, with or without its
    /// extension, made safe for a file name.</summary>
    public static string SolutionFileName(string name)
    {
        var trimmed = name.Trim();
        if (trimmed.EndsWith(SolutionExtension, StringComparison.OrdinalIgnoreCase))
            trimmed = trimmed.Substring(0, trimmed.Length - SolutionExtension.Length);
        return WholeProjectDecompiler.CleanUpFileName(trimmed, SolutionExtension);
    }

    private const string SolutionExtension = ".sln";

    /// <summary><paramref name="name"/> made safe for a file and folder name.</summary>
    public static string CleanProjectName(string name) => WholeProjectDecompiler.CleanUpDirectoryName(name.Trim());

    /// <summary>A project of each of <paramref name="assemblies"/>, named after it, with a number
    /// after the names that other assemblies already took.</summary>
    public static List<SolutionProject> NameProjects(IReadOnlyList<LoadedAssembly> assemblies)
    {
        var taken = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var projects = new List<SolutionProject>();
        foreach (var assembly in assemblies)
        {
            var name = ReserveProjectName(taken, assembly.Name);
            projects.Add(new SolutionProject(assembly, name));
        }
        return projects;
    }

    private static string ReserveProjectName(HashSet<string> taken, string name)
    {
        var clean = CleanProjectName(name);
        var candidate = clean;
        var number = 2;
        while (!taken.Add(candidate))
        {
            candidate = $"{clean}_{number}";
            number++;
        }
        return candidate;
    }
    private static ProjectItem ToSolutionItem(ExportedProject project)
    {
        var id = project.Id!;
        return new ProjectItem(project.ProjectPath, id.PlatformName, id.Guid, id.TypeGuid);
    }

    internal static bool CanGeneratePdb(LoadedAssembly assembly) => PortablePdbWriter.HasCodeViewDebugDirectoryEntry(assembly.File);

    /// <summary>Decompiles <paramref name="assembly"/> and writes a portable PDB at <paramref name="path"/>
    /// that maps its IL to that code, with the code embedded. The file is replaced only once the PDB
    /// is whole.</summary>
    public static void GeneratePdb(LoadedAssembly assembly, DecompilerOptions options, string path,
        IProgress<ExportProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        GeneratePdb(assembly, options, path, embedSources: true, Part(progress, assembly, 0, WholeExport), cancellationToken);
    }

    private static void GeneratePdb(LoadedAssembly assembly, DecompilerOptions options, string path, bool embedSources,
        IProgress<DecompilationProgress>? progress, CancellationToken cancellationToken)
    {
        var temporary = path + ".tmp";
        try
        {
            WritePdb(assembly, options, temporary, embedSources, progress, cancellationToken);
            File.Move(temporary, path, overwrite: true);
        }
        finally
        {
            File.Delete(temporary);
        }
    }

    private static void WritePdb(LoadedAssembly assembly, DecompilerOptions options, string path, bool embedSources,
        IProgress<DecompilationProgress>? progress, CancellationToken cancellationToken)
    {
        var settings = Settings.Create(options);
        var decompiler = NodeDecompiler.CreateCSharpDecompiler(assembly, settings, cancellationToken);
        using var stream = new FileStream(path, FileMode.Create, FileAccess.Write);
        var writer = new PortablePdbWriter { Progress = progress, EmbedSourceFiles = embedSources };
        writer.WritePdb(assembly.File, decompiler, settings, stream);
    }

    /// <summary>Writes a portable PDB of each of <paramref name="assemblies"/> into
    /// <paramref name="folder"/>, named after the assembly, and lists the ones it could not.</summary>
    public static List<PdbFailure> GeneratePdbs(IReadOnlyList<LoadedAssembly> assemblies, DecompilerOptions options, string folder,
        IProgress<ExportProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        var bounds = PartBounds(assemblies);
        var failures = new List<PdbFailure>();
        for (var i = 0; i < assemblies.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var assembly = assemblies[i];
            var part = Part(progress, assembly, i, bounds);
            var failure = TryGeneratePdb(assembly, options, folder, embedSources: true, part, cancellationToken);
            if (failure != null)
                failures.Add(failure);
        }
        return failures;
    }

    private static PdbFailure? TryGeneratePdb(LoadedAssembly assembly, DecompilerOptions options, string folder, bool embedSources,
        IProgress<DecompilationProgress>? progress, CancellationToken cancellationToken)
    {
        if (!assembly.CanGeneratePdb)
            return new PdbFailure(assembly, null);
        var path = Path.Combine(folder, assembly.PdbFileName);
        try
        {
            GeneratePdb(assembly, options, path, embedSources, progress, cancellationToken);
            return null;
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            return new PdbFailure(assembly, new ExportError(e.Message, e.ToString()));
        }
    }

    /// <summary>Where the one part of an export of a single assembly starts and ends in it.</summary>
    private static readonly float[] WholeExport = [0f, 1f];

    /// <summary>Where each part of an export of <paramref name="assemblies"/> starts in it, and where
    /// the last ends: each assembly takes the share of its types among all of theirs.</summary>
    internal static float[] PartBounds(IReadOnlyList<LoadedAssembly> assemblies)
    {
        var bounds = new float[assemblies.Count + 1];
        var total = assemblies.Sum(WeightOf);
        var before = 0;
        for (var i = 0; i < assemblies.Count; i++)
        {
            bounds[i] = (float)before / total;
            before += WeightOf(assemblies[i]);
        }
        bounds[assemblies.Count] = 1f;
        return bounds;
    }

    private static int WeightOf(LoadedAssembly assembly)
    {
        var types = assembly.File.Metadata.TypeDefinitions.Count;
        return Math.Max(1, types);
    }

    /// <summary>Tells <paramref name="progress"/> that the part at <paramref name="index"/>,
    /// <paramref name="assembly"/>, starts where <paramref name="bounds"/> say, and gives what reports
    /// the decompiler's progress within it.</summary>
    private static PartProgress? Part(IProgress<ExportProgress>? progress, LoadedAssembly assembly, int index, float[] bounds)
    {
        if (progress == null)
            return null;
        var part = new PartProgress(progress, assembly.Name, index + 1, bounds.Length - 1, bounds[index], bounds[index + 1]);
        part.Start();
        return part;
    }

    /// <summary>The decompiler's progress within one part of an export, which spans
    /// <paramref name="from"/> to <paramref name="to"/> of the whole, passed on as the export's.</summary>
    private sealed class PartProgress(IProgress<ExportProgress> progress, string name, int part, int parts, float from, float to)
        : IProgress<DecompilationProgress>
    {
        public void Start() => progress.Report(new ExportProgress(name, part, parts, from));

        /// <summary>This part's span cut in two, for two steps that each take half of it.</summary>
        public (PartProgress First, PartProgress Second) Halves()
        {
            var middle = (from + to) / 2;
            var first = new PartProgress(progress, name, part, parts, from, middle);
            var second = new PartProgress(progress, name, part, parts, middle, to);
            return (first, second);
        }

        public void Report(DecompilationProgress value)
        {
            var fraction = FractionAt(value.UnitsCompleted, value.TotalUnits);
            progress.Report(new ExportProgress(name, part, parts, fraction));
        }

        private float FractionAt(int done, int total)
        {
            if (total <= 0)
                return from;
            var share = Math.Clamp((float)done / total, 0f, 1f);
            return from + (to - from) * share;
        }
    }
}