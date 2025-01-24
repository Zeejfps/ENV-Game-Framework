namespace EasyGameFramework.GUI;

public interface IBuildContext
{
    IPanelRenderer PanelRenderer { get; }
    ITextRenderer TextRenderer { get; }
    FocusTree FocusTree { get; }
}