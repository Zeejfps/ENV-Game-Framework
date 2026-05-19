using ZGF.Gui;

namespace GitGui;

public interface IGroupSectionView
{
    /// <summary>Install the section's header view.</summary>
    void SetHeader(View header);

    /// <summary>
    /// Wire the repo row list to a derived compute and a factory for each row. Auto-tracked
    /// observable reads inside <paramref name="compute"/> re-run the compute on invalidation.
    /// </summary>
    void BindRows(Func<IEnumerable<Repo>> compute, Func<Repo, View> rowFactory);
}
