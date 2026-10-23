using ICSharpCode.Decompiler.CSharp.ProjectDecompiler;
using ICSharpCode.Decompiler.DebugInfo;
using ICSharpCode.Decompiler.Solution;

namespace ZGF.ILSpy.Core;

/// <summary>An assembly decompiled into a C# project: the project file, and what could not be
/// decompiled, which the project holds as error comments instead.</summary>
public sealed record ExportedProject(LoadedAssembly Assembly, string ProjectPath, IReadOnlyList<string> Errors)
{
    internal ProjectId? Id { get; init; }
}

/// <summary>Writing assemblies out as C# projects, solutions and portable PDBs.</summary>
internal static class ProjectExport
{
    public const string ProjectExtension = ".csproj";
    public const string PdbExtension = ".pdb";

    /// <summary>Decompiles <paramref name="assembly"/> into a project whose file is
    /// <paramref name="projectPath"/>, with its code and resources in the folder beside it.</summary>
    public static ExportedProject ExportProject(LoadedAssembly assembly, DecompilerOptions options, string projectPath, CancellationToken cancellationToken)
    {
        var folder = Path.GetDirectoryName(projectPath)!;
        Directory.CreateDirectory(folder);
        var decompiler = CreateProjectDecompiler(assembly, options);
        using var projectFile = new StreamWriter(projectPath);
        var id = decompiler.DecompileProject(assembly.File, folder, projectFile, cancellationToken);
        var errors = decompiler.Errors
            .Select(static e => e.Message)
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
        CancellationToken cancellationToken)
    {
        var folder = Path.GetDirectoryName(solutionPath)!;
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var projects = new List<ExportedProject>();
        foreach (var assembly in assemblies)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var name = ReserveProjectName(names, assembly.Name);
            var projectPath = Path.Combine(folder, name, name + ProjectExtension);
            projects.Add(ExportProject(assembly, options, projectPath, cancellationToken));
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

    /// <summary>Whether <paramref name="assembly"/> names a PDB in its debug directory, which a
    /// generated PDB must match for debuggers to load it.</summary>
    public static bool CanGeneratePdb(LoadedAssembly assembly) => PortablePdbWriter.HasCodeViewDebugDirectoryEntry(assembly.File);

    /// <summary>Decompiles <paramref name="assembly"/> and writes a portable PDB at <paramref name="path"/>
    /// that maps its IL to that code, with the code embedded. The file is replaced only once the PDB
    /// is whole.</summary>
    public static void GeneratePdb(LoadedAssembly assembly, DecompilerOptions options, string path, CancellationToken cancellationToken)
    {
        var temporary = path + ".tmp";
        try
        {
            WritePdb(assembly, options, temporary, cancellationToken);
            File.Move(temporary, path, overwrite: true);
        }
        finally
        {
            File.Delete(temporary);
        }
    }

    private static void WritePdb(LoadedAssembly assembly, DecompilerOptions options, string path, CancellationToken cancellationToken)
    {
        var settings = Settings.Create(options);
        var decompiler = NodeDecompiler.CreateCSharpDecompiler(assembly, settings, cancellationToken);

        using var stream = new FileStream(path, FileMode.Create, FileAccess.Write);
        var writer = new PortablePdbWriter();
        writer.WritePdb(assembly.File, decompiler, settings, stream);
    }
}
