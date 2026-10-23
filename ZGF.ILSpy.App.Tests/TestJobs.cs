namespace ZGF.ILSpy.App.Tests;

/// <summary>Export jobs for tests that only need the queue to hold something by a name.</summary>
internal static class TestJobs
{
    public static ExportJob Named(string name) => new(name, ExportKind.Project, Path.GetTempPath());
}
