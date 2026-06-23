using ZGF.Observable;

namespace ZGF.Gui.Localization;

/// <summary>
/// Runtime-switchable string catalog, mirroring <see cref="IThemeService{TStyles}"/>. An app's
/// generated <c>Strings</c> type is <typeparamref name="TStrings"/>; the catalog re-renders the
/// widget tree on a locale switch because <see cref="Strings"/> is an observable.
/// </summary>
public interface ILocalizationService<TStrings>
{
    IReadable<TStrings> Strings { get; }
}
