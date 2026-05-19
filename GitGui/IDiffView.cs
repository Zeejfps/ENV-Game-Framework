using ZGF.Observable;

namespace GitGui;

public record DiffTarget(string Path, DiffSide Side);

public interface IDiffView
{
    /// <summary>Current diff target — <c>null</c> means nothing is selected.</summary>
    IReadable<DiffTarget?> Target { get; }

    /// <summary>Push render state into the view.</summary>
    void SetViewModel(DiffViewModel vm);
}
