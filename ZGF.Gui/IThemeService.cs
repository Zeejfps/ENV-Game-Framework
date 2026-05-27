using ZGF.Observable;

namespace ZGF.Gui;

public interface IThemeService<TStyles>
{
    IReadable<TStyles> Styles { get; }
}
