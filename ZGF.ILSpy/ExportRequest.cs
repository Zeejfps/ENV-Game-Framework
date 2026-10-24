using ZGF.ILSpy.Core;

namespace ZGF.ILSpy;

/// <summary>An export of open assemblies as the export dialog asks for it: the folder it goes into,
/// the projects, what each is written with besides its code, and for several, the solution's file name.</summary>
public sealed record ExportRequest(string Folder, List<SolutionProject> Projects, ProjectExtras Extras, string SolutionFileName)
{
    /// <summary>Whether the export writes a solution: there is more than one project.</summary>
    public bool IsSolution => Projects.Count > 1;
}
