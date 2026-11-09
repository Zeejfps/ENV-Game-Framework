namespace ZGF.Gui.Automation;

/// <summary>Where <see cref="RemoteApp.Look(string)"/> writes: <c>scratch/</c> at the root of the repository the tests
/// were built from, else in the temp folder, as an in-process harness's <c>Look</c> does.</summary>
internal static class ScratchFolder
{
    public static string Find()
    {
        var root = RepositoryRoot(AppContext.BaseDirectory) ?? RepositoryRoot(Environment.CurrentDirectory);
        return root != null
            ? Path.Combine(root, "scratch")
            : Path.Combine(Path.GetTempPath(), "zgf-scratch");
    }

    /// <summary>The nearest folder at or above <paramref name="start"/> holding <c>.git</c>, which a worktree has as a file.</summary>
    private static string? RepositoryRoot(string start)
    {
        var directory = new DirectoryInfo(start);
        while (directory != null)
        {
            var git = Path.Combine(directory.FullName, ".git");
            if (Directory.Exists(git) || File.Exists(git))
                return directory.FullName;
            directory = directory.Parent;
        }
        return null;
    }
}
