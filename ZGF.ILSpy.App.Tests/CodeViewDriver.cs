using ZGF.Desktop;
using ZGF.Gui.Immediate.Testing;
using ZGF.Gui.Immediate.Widgets;
using ZGF.Gui.Immediate;
using ZGF.ILSpy.App.Tests.Fixtures;
using ZGF.ILSpy.Core;
using ZGF.KeyboardModule;

namespace ZGF.ILSpy.App.Tests;

/// <summary>Runs a code view filling a window, with the synthetic metrics of <see cref="UiFrames"/>:
/// cells 8 by 16 centered in rows of the theme's code line height, the view's top-left at the window
/// padding (16, 16), and a gutter of a 16 wide bookmark column + 8 + digits + 8 (+ a 16 wide fold
/// column), then 12 before the text.</summary>
internal sealed class CodeViewDriver
{
    public const float Origin = 16f;
    public const float Cell = 8f;
    private const float CellHeight = 16f;
    private const float GutterPadding = 8f;
    private const float FoldColumn = 16f;
    private const float MarkColumn = 16f;
    private const float CodePadding = 12f;

    public static readonly float Row = UiTheme.Dark.CodeLineHeight;
    private static readonly float CellInset = MathF.Floor((Row - CellHeight) / 2);

    private static readonly Lazy<DecompiledDocument> s_greeter = new(() => Decompile(typeof(Greeter)));
    private static readonly Lazy<DecompiledDocument> s_outline = new(() => Decompile(typeof(Outline)));

    public CodeViewDriver(DecompiledDocument document, float height = 300f)
    {
        Document = document;
        Frames = new UiFrames(Screens.Blank(), height: height, body: Render) { FrameTime = 1f / 60f };
    }

    private void Render(Ui<UiState> ui)
    {
        using var screen = ui.Screen();
        Last = ui.CodeView("code", Document, State, Bookmarks, tooltips: Tooltips);
    }

    /// <summary>A driver that has run <paramref name="frames"/> frames of <paramref name="document"/>.</summary>
    public static CodeViewDriver Showing(DecompiledDocument document, float height = 300f, int frames = 1)
    {
        var view = new CodeViewDriver(document, height);
        view.Run(frames);
        return view;
    }

    public UiFrames Frames { get; }
    public CodeViewState State { get; } = new();
    public DecompiledDocument Document { get; set; }
    public IReadOnlyDictionary<int, bool>? Bookmarks { get; set; }
    public CodeTooltips? Tooltips { get; set; }
    public CodeViewResult Last { get; private set; }

    private float FoldLeft => Origin + MarkColumn + GutterPadding + Math.Max(3, CodeTextDigits(Document.Lines.Length)) * Cell + GutterPadding;

    public float TextLeft => FoldLeft + (Document.Folds.Length > 0 ? FoldColumn : 0f) + CodePadding;

    /// <summary>Where the bookmark mark of the row at <paramref name="row"/> is.</summary>
    public (float X, float Y) MarkOf(int row) => (Origin + (MarkColumn + GutterPadding) / 2, Origin + row * Row + Row / 2);

    /// <summary>Where the fold chevron of the row at <paramref name="row"/> is.</summary>
    public (float X, float Y) ChevronOf(int row) => (FoldLeft + FoldColumn / 2, Origin + row * Row + Row / 2);

    public CodeViewResult Run(int frames = 1)
    {
        Frames.Frame(frames);
        return Last;
    }

    public (float X, float Y) PointOf(int line, int column) => (TextLeft + column * Cell + 1f, Origin + line * Row + Row / 2);

    public UiFrames MoveTo(int line, int column)
    {
        var (x, y) = PointOf(line, column);
        return Frames.MovePointer(x, y).Frame();
    }

    /// <summary>Clicks at the cell, as <see cref="Click(ValueTuple{float, float}, PointerButton, KeyModifiers)"/>.</summary>
    public CodeViewResult Click(int line, int column, PointerButton button = PointerButton.Left, KeyModifiers modifiers = KeyModifiers.None) =>
        Click(PointOf(line, column), button, modifiers);

    /// <summary>Clicks at <paramref name="at"/> and returns what the press asked for, or else what the release did.</summary>
    public CodeViewResult Click((float X, float Y) at, PointerButton button = PointerButton.Left, KeyModifiers modifiers = KeyModifiers.None)
    {
        Frames.MovePointer(at.X, at.Y).Frame();
        Frames.Press(button, modifiers).Frame();
        var pressed = Last;
        Frames.Release(button, modifiers).Frame();
        return pressed != default ? pressed : Last;
    }

    public UiFrames Key(KeyboardKey key, KeyModifiers modifiers = KeyModifiers.None) => Frames.Key(key, modifiers).Frame();

    /// <summary>The lines whose text was drawn, by the top of the row they were drawn in.</summary>
    public IEnumerable<(string Text, float Top)> DrawnText() =>
        Frames.Canvas.GlyphRuns.Where(r => r.Origin.X >= TextLeft - 0.5f).Select(r => (r.Text, RowTop(r.Origin.Y)));

    /// <summary>The top of the row a glyph run drawn at canvas height <paramref name="y"/> sits in.</summary>
    public float RowTop(float y) => Frames.Height - y - CellInset;

    public static DecompiledDocument Numbered(int count, string prefix = "line ") =>
        DecompiledDocument.FromText("numbered", string.Join('\n', Enumerable.Range(0, count).Select(i => prefix + i)));

    public static DecompiledDocument Greeter() => s_greeter.Value;

    public static DecompiledDocument Outline() => s_outline.Value;

    /// <summary>A document of <paramref name="type"/> decompiled through an assembly list of its own.</summary>
    public static DecompiledDocument Decompile(Type type)
    {
        var assembly = new AssemblyList().Open(type.Assembly.Location);
        var node = assembly.Root.Child(type.Namespace!, type.Name);
        return node.Decompile(DecompilationLanguage.CSharp);
    }

    private static int CodeTextDigits(int value) => value.ToString().Length;
}
