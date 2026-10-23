using ZGF.ILSpy.App.Tests.Fixtures;
using ZGF.ILSpy.Core;

namespace ZGF.ILSpy.App.Tests;

public sealed class CodeViewTooltipTests
{
    private const int MaxPolls = 200;
    private const double PollSeconds = 0.05;

    private static CodeViewDriver Farewell()
    {
        var list = new AssemblyList();
        var type = typeof(Farewell);
        var node = list.Open(type.Assembly.Location).Root.Child(type.Namespace!, type.Name);
        var view = new CodeViewDriver(node.Decompile(DecompilationLanguage.CSharp)) { Tooltips = new CodeTooltips(list) };
        view.Run();
        return view;
    }

    /// <summary>Runs frames until <paramref name="text"/> is drawn, as the lookup runs in the background.</summary>
    private static bool DrawsSoon(CodeViewDriver view, string text)
    {
        for (var i = 0; i < MaxPolls; i++)
        {
            view.Frames.Wait(PollSeconds).Frame();
            if (view.Frames.Drew(text))
                return true;
            Thread.Sleep(5);
        }
        return false;
    }

    private static void RestOn(CodeViewDriver view, string text)
    {
        var (line, column) = view.Document.Find(text);
        view.MoveTo(line, column + 1);
    }

    [Fact]
    public void Resting_on_a_member_shows_its_declaration_and_documentation()
    {
        var view = Farewell();

        RestOn(view, "Say()");

        Assert.True(DrawsSoon(view, "Says "));
        Assert.True(view.Frames.Drew("Bye"));
        Assert.Contains(view.Frames.Canvas.Texts, t => t.Inputs.Text.Contains("ZGF.ILSpy.App.Tests.Fixtures."));
        var summary = view.Frames.Canvas.Texts.Single(t => t.Inputs.Text == "Says ");
        var code = view.Frames.Canvas.Texts.Single(t => t.Inputs.Text == "Bye");
        Assert.NotEqual(MonoFonts.Regular, summary.Inputs.Style.FontFamily.Value);
        Assert.Equal(MonoFonts.Regular, code.Inputs.Style.FontFamily.Value);
    }

    [Fact]
    public void The_tooltip_waits_for_the_pointer_to_rest_and_closes_when_it_leaves()
    {
        var view = Farewell();
        RestOn(view, "Say()");
        Assert.True(DrawsSoon(view, "Says "));

        view.MoveTo(0, 0);
        view.Run(2);

        Assert.False(view.Frames.Drew("Says "));
    }

    [Fact]
    public void A_keyword_has_no_tooltip()
    {
        var view = Farewell();

        RestOn(view, "public class");

        Assert.False(DrawsSoon(view, "Says "));
    }
}
