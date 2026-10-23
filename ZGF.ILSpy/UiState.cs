using ZGF.Gui.Immediate;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

/// <summary>What every view is drawn with this frame: the theme, the editor's and icons' colors,
/// and the strings of the chosen language.</summary>
public sealed class UiState : IUiState
{
    public UiTheme Theme { get; set; } = UiTheme.Dark;
    public CodeViewPalette CodeViewPalette { get; set; } = CodeViewPalette.Dark;
    public NodeIconPalette NodeIconPalette { get; set; } = NodeIconPalette.Dark;
    public Strings Strings { get; set; } = Strings.En;
}
