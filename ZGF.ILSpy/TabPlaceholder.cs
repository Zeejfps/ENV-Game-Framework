using System.Diagnostics;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

/// <summary>The note a tab's code view shows while the tab has no code of its own, in the interface's
/// language, written again only when what it says changes.</summary>
public sealed class TabPlaceholder
{
    private TabContent? _content;
    private Strings? _strings;
    private DecompiledDocument _document = DecompiledDocument.Empty;

    /// <summary>The note for <paramref name="content"/>, written with <paramref name="t"/>.</summary>
    public DecompiledDocument For(TabContent content, Strings t)
    {
        if (IsWritten(content, t))
            return _document;
        _content = content;
        _strings = t;
        _document = DecompiledDocument.FromText("", $"// {Write(t, content)}");
        return _document;
    }

    private bool IsWritten(TabContent content, Strings t)
    {
        if (_strings != t)
            return false;
        return Equals(_content, content);
    }

    /// <summary>What the note for <paramref name="content"/> says.</summary>
    internal static string Write(Strings t, TabContent content) => content switch
    {
        TabContent.Loading => t.DocumentDecompiling,
        TabContent.Failed failed => t.DocumentDecompilationFailed(failed.Message),
        TabContent.Reloading => t.DocumentReloading,
        TabContent.Gone gone => t.DocumentGone(gone.Location.Text),
        _ => throw new UnreachableException(),
    };
}
