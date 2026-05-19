using ZGF.Gui;
using ZGF.Observable;

namespace GitGui;

public interface IRepoBarView
{
    /// <summary>
    /// Wires the section list to a source of groups and a factory that turns each
    /// group into a section view. Called once by the presenter.
    /// </summary>
    void BindGroups(ObservableList<Group> groups, Func<Group, View> sectionFactory);

    /// <summary>Raised when the user picks "New group" from the background context menu.</summary>
    event Action NewGroupRequested;
}
