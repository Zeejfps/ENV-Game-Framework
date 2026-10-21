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
/// padding (16, 16), and a gutter of 8 + digits + 8 (+ a 16 wide fold column), then 12 before the
/// text.</summary>
internal sealed class CodeViewDriver
{
    public const float Origin = 16f;
    public const float Cell = 8f;
    private const float CellHeight = 16f;
    private const float GutterPadding = 8f;
    private const float FoldColumn = 16f;
    private const float CodePadding = 12f;
    private const float Frame = 1f / 60f;

    public static readonly float Row = UiTheme.Dark.CodeLineHeight;
    private static readonly float CellInset = MathF.Floor((Row - CellHeight) / 2);

    private readonly Func<Ui, CodeViewResult> _body;

    public CodeViewDriver(DecompiledDocument document, float height = 300f)
    {
        Document = document;
        Frames = new UiFrames(height: height);
        _body = ui =>
        {
            using var screen = ui.Screen();
            return ui.CodeView("code", Document, State);
        };
    }

    public UiFrames Frames { get; }
    public CodeViewState State { get; } = new();
    public DecompiledDocument Document { get; set; }
    public CodeViewResult Last { get; private set; }

    private float FoldLeft => Origin + GutterPadding + Math.Max(3, CodeTextDigits(Document.Lines.Length)) * Cell + GutterPadding;

    public float TextLeft => FoldLeft + (Document.Folds.Length > 0 ? FoldColumn : 0f) + CodePadding;

    /// <summary>Where the fold chevron of the row at <paramref name="row"/> is.</summary>
    public (float X, float Y) ChevronOf(int row) => (FoldLeft + FoldColumn / 2, Origin + row * Row + Row / 2);

    public CodeViewResult Run()
    {
        Last = Frames.Run(Frame, _body);
        return Last;
    }

    public (float X, float Y) PointOf(int line, int column) => (TextLeft + column * Cell + 1f, Origin + line * Row + Row / 2);

    public void MoveTo(int line, int column)
    {
        var (x, y) = PointOf(line, column);
        Frames.MovePointer(x, y);
        Run();
    }

    /// <summary>Clicks at the cell and returns the first navigation any of the frames asked for.</summary>
    public CodeViewResult Click(int line, int column, PointerButton button = PointerButton.Left, KeyModifiers modifiers = KeyModifiers.None)
    {
        MoveTo(line, column);
        Frames.Press(button, modifiers);
        var pressed = Run();
        Frames.Release(button, modifiers);
        var released = Run();
        return pressed.Navigate != null ? pressed : released;
    }

    public void Key(KeyboardKey key, KeyModifiers modifiers = KeyModifiers.None)
    {
        Frames.Key(key, modifiers);
        Run();
    }

    /// <summary>The lines whose text was drawn, by the top of the row they were drawn in.</summary>
    public IEnumerable<(string Text, float Top)> DrawnText() =>
        Frames.Canvas.GlyphRuns.Where(r => r.Origin.X >= TextLeft - 0.5f).Select(r => (r.Text, RowTop(r.Origin.Y)));

    /// <summary>The top of the row a glyph run drawn at canvas height <paramref name="y"/> sits in.</summary>
    public float RowTop(float y) => Frames.Height - y - CellInset;

    public static DecompiledDocument Numbered(int count, string prefix = "line ") =>
        DecompiledDocument.FromText("numbered", string.Join('\n', Enumerable.Range(0, count).Select(i => prefix + i)));

    public static DecompiledDocument Greeter() => Decompile(typeof(Greeter));

    public static DecompiledDocument Outline() => Decompile(typeof(Outline));

    private static DecompiledDocument Decompile(Type type)
    {
        var list = new AssemblyList();
        var assembly = list.Open(type.Assembly.Location);
        var ns = assembly.Root.Children.Single(c => c.Text == type.Namespace);
        return ns.Children.Single(c => c.Text == type.Name).Decompile(DecompilationLanguage.CSharp);
    }

    private static int CodeTextDigits(int value) => value.ToString().Length;
}

internal static class DocumentQueries
{
    public static (int Line, int Column) Find(this DecompiledDocument document, string text, int skip = 0)
    {
        for (var i = 0; i < document.Lines.Length; i++)
        {
            var at = document.Lines[i].Text.IndexOf(text, StringComparison.Ordinal);
            if (at < 0) continue;
            if (skip-- == 0) return (i, at);
        }
        throw new InvalidOperationException($"'{text}' is not in the document:\n{document.GetText()}");
    }

    public static DocumentSpan SpanAt(this DecompiledDocument document, int line, int column)
    {
        foreach (var span in document.Lines[line].Spans)
        {
            if (span.Start <= column && column < span.End) return span;
        }
        throw new InvalidOperationException("No span there.");
    }
}
