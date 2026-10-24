using ZGF.ILSpy.Core;

namespace ZGF.ILSpy;

public sealed partial class UiState
{
    /// <summary>Docks the search panel under the documents if it is closed, brings it to the front
    /// and puts the caret in its search box.</summary>
    public void OpenSearch()
    {
        MainLayout.OpenSearch(FindActivePanel());
        IsSearchFocusPending = true;
    }

    /// <summary>Opens the search panel with its query kept to <paramref name="node"/>'s assembly, or to
    /// the namespace it is.</summary>
    public void ScopeSearch(BrowseNode node)
    {
        Search.ScopeTo(node);
        OpenSearch();
    }

    /// <summary>True once after <see cref="OpenSearch"/>: the search box should take focus.</summary>
    public bool TakeSearchFocus()
    {
        var focus = IsSearchFocusPending;
        IsSearchFocusPending = false;
        return focus;
    }

    /// <summary>Opens the export dialog on the open assemblies, with <paramref name="picked"/> ticked.</summary>
    public void OpenExportSetup(LoadedAssembly? picked)
    {
        ExportSetup.Open(_appState.Assemblies.Assemblies, picked);
    }

    /// <summary>Whether a dialog that keeps the window's shortcuts to itself is open.</summary>
    public bool IsDialogOpen()
    {
        if (Picker.IsOpen)
            return true;
        if (ErrorDialog.IsOpen)
            return true;
        return ExportSetup.IsOpen;
    }
}
