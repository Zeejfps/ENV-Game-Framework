using System.Diagnostics;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Core.Comparison;
using static ZGF.ILSpy.Navigation;

namespace ZGF.ILSpy;

/// <summary>Comparisons of two assemblies' APIs, each in a tab of its own: comparing them, swapping their
/// sides, and taking in more or less of them. Each comparison runs in the background against the
/// assemblies open then, and is dropped when another starts or its tab closes meanwhile.</summary>
public static class Comparisons
{
    /// <summary>Compares <paramref name="left"/> with <paramref name="right"/>, in the active tab while it
    /// shows nothing, else in a new tab, titled <paramref name="title"/>.</summary>
    public static void CompareAssemblies(AppState appState, LoadedModule left, LoadedModule right, string title)
    {
        LeaveActiveTab(appState);
        var tab = FindOrOpenEmptyTab(appState);
        var comparison = new AssemblyComparison(left.FilePath, right.FilePath);
        tab.ShowComparison(title, comparison);
        appState.Tabs.Keep(tab);
        appState.Tabs.MakeActive(tab);
        Compare(appState, comparison);
    }

    /// <summary>Shows the comparison <paramref name="saved"/> kept in the tab <paramref name="reopened"/> waits in,
    /// and compares its assemblies again, now that the open list's assemblies are listed.</summary>
    internal static void ReopenComparison(AppState appState, ReopenedTab reopened, ComparisonDocumentRecord saved)
    {
        if (!appState.Reopened.Finish(reopened))
            return;
        var comparison = new AssemblyComparison(saved.LeftPath, saved.RightPath);
        comparison.TakeIn(saved.Scope);
        var title = saved.Title ?? string.Empty;
        reopened.Tab.ShowComparison(title, comparison);
        Compare(appState, comparison);
    }

    /// <summary>Compares the assemblies of <paramref name="comparison"/> the other way round, the right one on the left.</summary>
    public static void SwapComparedAssemblies(AppState appState, AssemblyComparison comparison)
    {
        comparison.Swap();
        Compare(appState, comparison);
    }

    /// <summary>Compares the assemblies of <paramref name="comparison"/> again, taking in what <paramref name="scope"/> says.</summary>
    public static void ChangeComparedScope(AppState appState, AssemblyComparison comparison, ApiScope scope)
    {
        comparison.TakeIn(scope);
        Compare(appState, comparison);
    }

    /// <summary>Compares the assemblies of <paramref name="comparison"/>, as they are open now, in the
    /// background, and shows what changed unless another comparison started or it was closed meanwhile.
    /// Shows why when either is no longer open or the comparison fails.</summary>
    private static async void Compare(AppState appState, AssemblyComparison comparison)
    {
        var left = FindOpen(appState, comparison.LeftPath);
        if (left == null)
        {
            ShowClosed(appState, comparison, comparison.LeftPath);
            return;
        }
        var right = FindOpen(appState, comparison.RightPath);
        if (right == null)
        {
            ShowClosed(appState, comparison, comparison.RightPath);
            return;
        }
        var scope = comparison.Scope;
        var ticket = comparison.StartComparing();
        var compared = await Background.Run(() => ApiDiff.Compare(left, right, scope, ticket.Token));
        if (!comparison.FinishComparing(ticket))
            return;
        switch (compared)
        {
            case Outcome<ApiDiff>.Ok ok:
                comparison.Show(ok.Value);
                break;
            case Outcome<ApiDiff>.Failed failed:
                ReportFailure(appState, comparison, left, right, failed.Error);
                break;
            default:
                throw new UnreachableException();
        }
    }

    /// <summary>The open assembly read from <paramref name="filePath"/> whose API can be compared; null when there is none.</summary>
    private static LoadedModule? FindOpen(AppState appState, string filePath)
    {
        foreach (var assembly in appState.Assemblies.Assemblies)
        {
            if (string.Equals(assembly.FilePath, filePath, StringComparison.OrdinalIgnoreCase))
                return assembly;
        }
        return null;
    }

    private static void ShowClosed(AppState appState, AssemblyComparison comparison, string filePath)
    {
        comparison.ShowFailure(new ComparisonFailure.AssemblyClosed(Path.GetFileName(filePath)));
    }

    private static void ReportFailure(AppState appState, AssemblyComparison comparison, LoadedModule left, LoadedModule right, Exception error)
    {
        comparison.ShowFailure(new ComparisonFailure.Failed(left.NameAndVersion, right.NameAndVersion));
        appState.ReportError(new AppProblem.ComparisonFailed(left.NameAndVersion, right.NameAndVersion, error));
    }
}
