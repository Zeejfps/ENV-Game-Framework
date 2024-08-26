namespace OpenGLSandbox;

public interface IBuildContext
{
    IPanelRenderer PanelRenderer { get; }
    ITextRenderer TextRenderer { get; }
    FocusTree FocusTree { get; }
}