using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp.ProjectDecompiler;
using ICSharpCode.Decompiler.DebugInfo;
using ICSharpCode.Decompiler.Solution;

namespace ZGF.ILSpy.Core;

/// <summary>Something the decompiler could not handle: what it was doing, and the full error.</summary>
public sealed record ExportError(string Message, string Details);

/// <summary>An assembly decompiled into a C# project: the project file, and what could not be
/// decompiled, which the project holds as error comments instead.</summary>
public sealed record ExportedProject(LoadedAssembly Assembly, string ProjectPath, IReadOnlyList<ExportError> Errors)
{
    internal ProjectId? Id { get; init; }
}

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
    /// <paramref name="projectPath"/>, with its code and resources in the folder beside it.</summary>
    public static ExportedProject ExportProject(LoadedAssembly assembly, DecompilerOptions options, string projectPath,
        IProgress<ExportProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        return ExportProject(assembly, options, projectPath, Part(progress, assembly, 0, WholeExport), cancellationToken);
    }

    private static ExportedProject ExportProject(LoadedAssembly assembly, DecompilerOptions options, string projectPath,
        IProgress<DecompilationProgress>? progress, CancellationToken cancellationToken)
    {
        var folder = Path.GetDirectoryName(projectPath)!;
        Directory.CreateDirectory(folder);
        var decompiler = CreateProjectDecompiler(assembly, options);
        decompiler.ProgressIndicator = progress;
        using var projectFile = new StreamWriter(projectPath);
        var id = decompiler.DecompileProject(assembly.File, folder, projectFile, cancellationToken);
        var errors = decompiler.Errors
            .Select(static e => new ExportError(e.Message, e.ToString()))
            .ToList();
        return new ExportedProject(assembly, projectPath, errors) { Id = id };
    }

    private static WholeProjectDecompiler CreateProjectDecompiler(LoadedAssembly assembly, DecompilerOptions options)
    {
        var settings = Settings.Create(options);
        var symbols = NodeDecompiler.DebugSymbolsFor(assembly, settings);
        return new WholeProjectDecompiler(settings, assembly.Resolver, null, null, symbols);
    }

    /// <summary>Decompiles each of <paramref name="assemblies"/> into a project in a folder of its own
    /// beside <paramref name="solutionPath"/>, then writes the solution that holds them.</summary>
    public static List<ExportedProject> ExportSolution(IReadOnlyList<LoadedAssembly> assemblies, DecompilerOptions options, string solutionPath,
        IProgress<ExportProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        var folder = Path.GetDirectoryName(solutionPath)!;
        var bounds = PartBounds(assemblies);
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var projects = new List<ExportedProject>();
        for (var i = 0; i < assemblies.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var assembly = assemblies[i];
            var name = ReserveProjectName(names, assembly.Name);
            var projectPath = Path.Combine(folder, name, name + ProjectExtension);
            var part = Part(progress, assembly, i, bounds);
            projects.Add(ExportProject(assembly, options, projectPath, part, cancellationToken));
        }
        var items = projects.ConvertAll(ToSolutionItem);
        SolutionCreator.WriteSolutionFile(solutionPath, items);
        return projects;
    }

    /// <summary>The folder name for a project of the assembly named <paramref name="name"/>, with a
    /// number after it when another assembly of the solution has the same name.</summary>
    private static string ReserveProjectName(HashSet<string> taken, string name)
    {
        var clean = WholeProjectDecompiler.CleanUpDirectoryName(name);
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
        GeneratePdb(assembly, options, path, Part(progress, assembly, 0, WholeExport), cancellationToken);
    }

    private static void GeneratePdb(LoadedAssembly assembly, DecompilerOptions options, string path, IProgress<DecompilationProgress>? progress,
        CancellationToken cancellationToken)
    {
        var temporary = path + ".tmp";
        try
        {
            WritePdb(assembly, options, temporary, progress, cancellationToken);
            File.Move(temporary, path, overwrite: true);
        }
        finally
        {
            File.Delete(temporary);
        }
    }

    private static void WritePdb(LoadedAssembly assembly, DecompilerOptions options, string path, IProgress<DecompilationProgress>? progress,
        CancellationToken cancellationToken)
    {
        var settings = Settings.Create(options);
        var decompiler = NodeDecompiler.CreateCSharpDecompiler(assembly, settings, cancellationToken);
        using var stream = new FileStream(path, FileMode.Create, FileAccess.Write);
        var writer = new PortablePdbWriter { Progress = progress };
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
            var failure = TryGeneratePdb(assembly, options, folder, part, cancellationToken);
            if (failure != null)
                failures.Add(failure);
        }
        return failures;
    }

    private static PdbFailure? TryGeneratePdb(LoadedAssembly assembly, DecompilerOptions options, string folder,
        IProgress<DecompilationProgress>? progress, CancellationToken cancellationToken)
    {
        if (!assembly.CanGeneratePdb)
            return new PdbFailure(assembly, null);
        var path = Path.Combine(folder, assembly.PdbFileName);
        try
        {
            GeneratePdb(assembly, options, path, progress, cancellationToken);
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
    private static IProgress<DecompilationProgress>? Part(IProgress<ExportProgress>? progress, LoadedAssembly assembly, int index, float[] bounds)
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