using ZGF.Gui;

namespace GitGui;

public interface IGroupHeaderRowView
{
    /// <summary>
    /// Wires the name slot. <paramref name="isRenamingCompute"/> is auto-tracked; when its
    /// observable dependencies invalidate, the slot is reseeded with the new factory output.
    /// </summary>
    void BindName(Func<IEnumerable<bool>> isRenamingCompute, Func<bool, View> contentFactory);

    /// <summary>
    /// Builds the right-click menu items. Presenter sets this; controller queries it on
    /// each right-click so menu contents reflect current registry state.
    /// </summary>
    Func<IReadOnlyList<RepoBarContextMenu.Item>>? MenuItemsProvider { set; }

    /// <summary>
    /// Tells the controller whether the group is currently being renamed. Used to suppress
    /// press/drag while the rename field is focused.
    /// </summary>
    Func<bool>? IsRenamingProvider { set; }

    /// <summary>Raised when the user left-clicks the header (collapse/expand).</summary>
    event Action ToggleCollapsedRequested;
}
