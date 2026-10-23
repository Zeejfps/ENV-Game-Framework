using ZGF.Gui.Immediate;
using ZGF.ILSpy.App.Tests.Fixtures;
using ZGF.ILSpy.Core;

namespace ZGF.ILSpy.App.Tests;

public sealed class CodeViewTooltipTests
{
    private const int MaxPolls = 200;
    private const double PollSeconds = 0.05;
    private const float WindowHeight = 700f;
    private const double LeaveGraceSeconds = 0.3;
    private const string Context = "ZGF.ILSpy.App.Tests.Fixtures.Farewell";

    private static CodeViewDriver Farewell()
    {
        var list = new AssemblyList();
        var type = typeof(Farewell);
        var node = list.Open(type.Assembly.Location).Root.Child(type.Namespace!, type.Name);
        var view = new CodeViewDriver(node.Decompile(DecompilationLanguage.CSharp), WindowHeight) { Tooltips = new CodeTooltips(list) };
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
        Assert.True(view.Frames.Drew(Context));
        var summary = view.Frames.Canvas.Texts.Single(t => t.Inputs.Text == "Says ");
        var code = view.Frames.Canvas.Texts.Single(t => t.Inputs.Text == "Bye");
        Assert.NotEqual(MonoFonts.Regular, summary.Inputs.Style.FontFamily.Value);
        Assert.Equal(MonoFonts.Regular, code.Inputs.Style.FontFamily.Value);
    }

    [Fact]
    public void A_signature_too_wide_for_one_line_puts_each_parameter_on_a_line_of_its_own()
    {
        var view = Farewell();

        RestOn(view, "Compose(");
        Assert.True(DrawsSoon(view, Context));
        var compose = TooltipText(view, "Compose");
        var greeting = TooltipText(view, "greeting");
        var name = TooltipText(view, "name");

        Assert.True(compose.Y < greeting.Y);
        Assert.True(greeting.Y < name.Y);
        Assert.Equal(greeting.X, name.X);
    }

    /// <summary>Where the tooltip drew <paramref name="text"/>: in the tooltip's text size, not the code's.</summary>
    private static UiRect TooltipText(CodeViewDriver view, string text)
    {
        var drawn = view.Frames.Canvas.Texts
            .Where(t => t.Inputs.Text == text)
            .Single(t => t.Inputs.Style.FontSize.Value != UiTheme.Dark.MonoFontSize);
        return view.Frames.ToUi(drawn.Inputs.Position);
    }

    [Fact]
    public void Clicking_a_type_in_the_tooltip_navigates_to_it_and_closes_the_tooltip()
    {
        var view = Farewell();
        RestOn(view, "Say()");
        Assert.True(DrawsSoon(view, "Says "));

        var type = TooltipText(view, "Farewell");
        var result = view.Click((type.X + 2f, type.Y + type.Height / 2));
        view.Run(2);

        Assert.Equal(CodeReferenceKind.Type, result.Navigate?.Kind);
        Assert.Equal("Farewell", result.Navigate?.Name);
        Assert.False(result.NavigateInNewTab);
        Assert.False(view.Frames.Drew("Says "));
    }

    [Fact]
    public void The_tooltip_waits_for_the_pointer_to_rest_and_closes_when_it_leaves()
    {
        var view = Farewell();
        RestOn(view, "Say()");
        Assert.True(DrawsSoon(view, "Says "));

        view.MoveTo(0, 0);
        view.Frames.Wait(LeaveGraceSeconds).Frame(2);

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
