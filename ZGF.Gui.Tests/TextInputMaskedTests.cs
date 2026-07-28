using System.Runtime.InteropServices;
using ZGF.Desktop.Input;
using ZGF.Fonts;
using ZGF.Gui.Desktop.Components.Controls;
using ZGF.Gui.Desktop.Components.TextInput;
using ZGF.Gui.Desktop.Input;
using ZGF.Gui.Testing;
using ZGF.KeyboardModule;
using ZGF.Observable;

namespace ZGF.Gui.Tests;

/// <summary>
/// Masking is a display decision and nothing else: the buffer, the clipboard and every editing
/// gesture behave as they do in an ordinary field, and only what reaches the canvas changes. So the
/// tests come in pairs — each one that pins the masked behaviour has a counterpart asserting an
/// ordinary field is untouched, which is what proves the change is scoped to the flag rather than to
/// the control. The measurement tests run on deliberately proportional metrics, because monospaced
/// ones would hide the bug this feature invites: measuring the plaintext while drawing bullets.
/// </summary>
public class TextInputMaskedTests
{
    private const char Bullet = TextInputView.MaskCharacter;

    private sealed class FakeClipboard : IClipboard
    {
        public string? Text;
        public void SetText(string text) => Text = text;
        public string? GetText() => Text;
    }

    private static readonly InputModifiers CommandModifier =
        RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? InputModifiers.Super : InputModifiers.Control;

    // Proportional metrics with one conspicuously wide character. A caret placed against the
    // plaintext's widths lands somewhere a caret placed against the bullets' never does, so the two
    // cannot be confused by a test that happens to pass on a monospaced measurer.
    private sealed class ProportionalMeasurer : ITextMeasurer
    {
        private const float WideWidth = 24f;
        private const float NarrowWidth = 8f;

        private static float WidthOf(char c) => c == 'W' ? WideWidth : NarrowWidth;

        public float MeasureTextWidth(ReadOnlySpan<char> text, TextStyle style)
        {
            var total = 0f;
            foreach (var c in text)
                total += WidthOf(c);
            return total;
        }

        public float MeasureTextPrefix(ReadOnlySpan<char> text, int prefixLength, TextStyle style) =>
            MeasureTextWidth(text[..Math.Clamp(prefixLength, 0, text.Length)], style);

        public float MeasureTextLineHeight(TextStyle style) => 16f;
    }

    private static GuiTestHarness Field(
        State<string> value, bool masked, FakeClipboard clipboard, out TextInputView view,
        bool readOnly = false, ITextMeasurer? measurer = null)
    {
        var h = GuiTestHarness.Create(
            ctx => new TextInput
            {
                Id = "field",
                Value = value,
                AutoFocus = true,
                Masked = masked,
                ReadOnly = readOnly,
                CaretColor = 0xFFFF0000,
            }.BuildView(ctx),
            configure: ctx => ctx.AddService<IClipboard>(clipboard),
            measurer: measurer);
        view = (TextInputView)h.Get("field");
        return h;
    }

    private static string DrawnText(GuiTestHarness h) =>
        string.Concat(h.Canvas.Texts.Select(t => t.Inputs.Text));

    // ---- what reaches the canvas ----

    [Fact]
    public void AMaskedFieldDrawsBulletsAndNeverItsText()
    {
        var value = new State<string>("sk-secret");
        using var h = Field(value, masked: true, new FakeClipboard(), out _);

        h.Render();

        var drawn = DrawnText(h);
        Assert.Equal(new string(Bullet, "sk-secret".Length), drawn);
        Assert.DoesNotContain("sk-secret", drawn);
        // Not even a fragment: a single leaked character of an API key is still a leaked character.
        Assert.DoesNotContain("sk", drawn);
    }

    [Fact]
    public void AnOrdinaryFieldStillDrawsItsRealText()
    {
        var value = new State<string>("sk-secret");
        using var h = Field(value, masked: false, new FakeClipboard(), out _);

        h.Render();

        Assert.Equal("sk-secret", DrawnText(h));
    }

    [Fact]
    public void TypingIntoAMaskedFieldDrawsOneMoreBulletAndNotTheCharacter()
    {
        var value = new State<string>(string.Empty);
        using var h = Field(value, masked: true, new FakeClipboard(), out _);

        h.Type("abc");
        h.Render();

        Assert.Equal("abc", value.Value);
        Assert.Equal(new string(Bullet, 3), DrawnText(h));
    }

    [Fact]
    public void APlaceholderIsNotMasked()
    {
        var value = new State<string>(string.Empty);
        using var h = GuiTestHarness.Create(ctx => new TextInput
        {
            Id = "field",
            Value = value,
            Masked = true,
            Placeholder = "API key",
        }.BuildView(ctx));

        h.Render();

        // The prompt is the app's own words, not the user's secret — masking it would just make the
        // field unlabelled.
        Assert.Equal("API key", string.Concat(h.Canvas.Texts.Select(t => t.Inputs.Text)));
    }

    // ---- caret and selection, measured against the drawn glyphs ----

    [Fact]
    public void DraggingOverAMaskedFieldSelectsByTheBulletWidths()
    {
        var value = new State<string>("WWWWWWWW");
        using var h = Field(value, masked: true, new FakeClipboard(), out var view,
            measurer: new ProportionalMeasurer());
        h.Layout();

        // 8px bullets: x=25 falls just past the third boundary. Had the field measured its plaintext
        // (24px per 'W') the same drag would have stopped after one character.
        Drag(h, view, fromX: 1f, toX: 25f);

        Assert.Equal("WWW", view.GetSelectedText());
    }

    [Fact]
    public void AnOrdinaryFieldStillSelectsByItsOwnGlyphWidths()
    {
        var value = new State<string>("WWWWWWWW");
        using var h = Field(value, masked: false, new FakeClipboard(), out var view,
            measurer: new ProportionalMeasurer());
        h.Layout();

        Drag(h, view, fromX: 1f, toX: 25f);

        Assert.Equal("W", view.GetSelectedText());
    }

    [Fact]
    public void TheCaretLandsOnTheBulletBoundaryItWasClickedAt()
    {
        var value = new State<string>("WWWWWWWW");
        using var h = Field(value, masked: true, new FakeClipboard(), out var view,
            measurer: new ProportionalMeasurer());
        h.Layout();

        var left = view.Position.Left;
        h.Click(left + 25f, view.Position.Top - 8f);

        // Three 8px bullets in: the caret sits where the fourth one starts on screen, not at the
        // 72px the plaintext's three 'W's would have occupied.
        Assert.Equal(left + 24f, view.GetCaretRect().Left, 0.01f);
    }

    [Fact]
    public void TheSelectionHighlightCoversTheBulletsAndNotThePlaintextsWidth()
    {
        var value = new State<string>("WWWWWWWW");
        using var h = Field(value, masked: true, new FakeClipboard(), out var view,
            measurer: new ProportionalMeasurer());

        view.SelectAll();
        h.Render();

        var selection = h.Canvas.Rects.Single(r => r.Inputs.Style.BackgroundColor == 0xFF8aadff);
        Assert.Equal(8 * 8f, selection.Inputs.Position.Width, 0.01f);
    }

    [Fact]
    public void CaretNavigationStillStepsOneCharacterAtATimeInAMaskedField()
    {
        var value = new State<string>("hello");
        using var h = Field(value, masked: true, new FakeClipboard(), out var view);

        h.PressKey(KeyboardKey.LeftArrow, InputModifiers.Shift);
        h.PressKey(KeyboardKey.LeftArrow, InputModifiers.Shift);

        Assert.Equal("lo", view.GetSelectedText());
    }

    // ---- editing is untouched ----

    [Fact]
    public void TypingPastingAndDeletingStillEditAMaskedField()
    {
        var value = new State<string>(string.Empty);
        var clipboard = new FakeClipboard { Text = "-tail" };
        using var h = Field(value, masked: true, clipboard, out var view);

        h.Type("head");
        Assert.Equal("head", value.Value);

        h.PressKey(KeyboardKey.V, CommandModifier);
        Assert.Equal("head-tail", value.Value);

        h.PressKey(KeyboardKey.Backspace);
        Assert.Equal("head-tai", value.Value);

        view.SelectAll();
        h.PressKey(KeyboardKey.Backspace);
        Assert.Equal(string.Empty, value.Value);
    }

    [Fact]
    public void UndoStillRewindsAMaskedField()
    {
        var value = new State<string>("hello");
        using var h = Field(value, masked: true, new FakeClipboard(), out _);

        h.Type("XYZ");
        h.PressKey(KeyboardKey.Z, CommandModifier);

        Assert.Equal("hello", value.Value);
    }

    [Fact]
    public void MaskedAndReadOnlyTogetherMaskAndRefuseEdits()
    {
        var value = new State<string>("hello");
        using var h = Field(value, masked: true, new FakeClipboard(), out _, readOnly: true);

        h.Type("XYZ");
        h.Render();

        Assert.Equal("hello", value.Value);
        Assert.Equal(new string(Bullet, 5), DrawnText(h));
        // Read-only draws no caret; masking must not have quietly brought it back.
        Assert.DoesNotContain(h.Canvas.Rects, r => r.Inputs.Style.BackgroundColor == 0xFFFF0000);
    }

    [Fact]
    public void AMaskedFieldStillDrawsItsCaretWhenItIsEditable()
    {
        var value = new State<string>("hello");
        using var h = Field(value, masked: true, new FakeClipboard(), out _);

        h.Render();

        Assert.Contains(h.Canvas.Rects, r => r.Inputs.Style.BackgroundColor == 0xFFFF0000);
    }

    // ---- the clipboard decision ----

    [Fact]
    public void CopyingFromAMaskedFieldPutsNothingOnTheClipboard()
    {
        // Masking is there because the value is a secret, and a clipboard is where secrets get pasted
        // somewhere they persist. The chord is declined rather than consumed, so it bubbles.
        var value = new State<string>("sk-live-1234");
        var clipboard = new FakeClipboard();
        using var h = Field(value, masked: true, clipboard, out _);

        h.PressKey(KeyboardKey.A, CommandModifier);
        h.PressKey(KeyboardKey.C, CommandModifier);

        Assert.Null(clipboard.Text);
    }

    // Cut refuses outright rather than deleting without copying, which would look like a cut and lose
    // the text — the user would paste an empty clipboard and find the key gone from both places.
    [Fact]
    public void CuttingFromAMaskedFieldNeitherCopiesNorDeletes()
    {
        var value = new State<string>("sk-live-1234");
        var clipboard = new FakeClipboard();
        using var h = Field(value, masked: true, clipboard, out _);

        h.PressKey(KeyboardKey.A, CommandModifier);
        h.PressKey(KeyboardKey.X, CommandModifier);

        Assert.Null(clipboard.Text);
        Assert.Equal("sk-live-1234", value.Value);
    }

    [Fact]
    public void AnOrdinaryFieldStillCopies()
    {
        var value = new State<string>("not-a-secret");
        var clipboard = new FakeClipboard();
        using var h = Field(value, masked: false, clipboard, out _);

        h.PressKey(KeyboardKey.A, CommandModifier);
        h.PressKey(KeyboardKey.C, CommandModifier);

        Assert.Equal("not-a-secret", clipboard.Text);
    }

    // ---- the plaintext's shape is a leak too ----

    [Fact]
    public void DoubleClickTakesTheWholeMaskedValueRatherThanAWordOfIt()
    {
        var value = new State<string>("key-with-parts");
        using var h = Field(value, masked: true, new FakeClipboard(), out var view);
        h.Layout();

        var point = (X: view.Position.Left + 12f, Y: view.Position.Top - 8f);
        h.Click(point.X, point.Y);
        h.Click(point.X, point.Y);

        Assert.Equal("key-with-parts", view.GetSelectedText());
    }

    [Fact]
    public void DoubleClickStillTakesOneWordFromAnOrdinaryField()
    {
        var value = new State<string>("key-with-parts");
        using var h = Field(value, masked: false, new FakeClipboard(), out var view);
        h.Layout();

        var point = (X: view.Position.Left + 12f, Y: view.Position.Top - 8f);
        h.Click(point.X, point.Y);
        h.Click(point.X, point.Y);

        Assert.Equal("key", view.GetSelectedText());
    }

    [Fact]
    public void WordJumpingSpansTheWholeMaskedValue()
    {
        var value = new State<string>("key-with-parts");
        using var h = Field(value, masked: true, new FakeClipboard(), out var view);

        var wordModifier = RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
            ? InputModifiers.Alt
            : InputModifiers.Control;
        h.PressKey(KeyboardKey.LeftArrow, wordModifier | InputModifiers.Shift);

        Assert.Equal("key-with-parts", view.GetSelectedText());
    }

    [Fact]
    public void WordJumpingStillStopsAtWordBoundariesInAnOrdinaryField()
    {
        var value = new State<string>("key-with-parts");
        using var h = Field(value, masked: false, new FakeClipboard(), out var view);

        var wordModifier = RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
            ? InputModifiers.Alt
            : InputModifiers.Control;
        h.PressKey(KeyboardKey.LeftArrow, wordModifier | InputModifiers.Shift);

        Assert.Equal("parts", view.GetSelectedText());
    }

    // ---- inspection surfaces ----

    [Fact]
    public void VisibleTextIsMaskedWhileTheBoundValueIsNot()
    {
        var value = new State<string>("sk-secret");
        using var h = Field(value, masked: true, new FakeClipboard(), out var view);

        // VisibleText is what automation (GuiDriver.TextOf) reports, so a scripted run's log holds
        // bullets. Text stays real — the binding that writes the key back through Value needs it.
        Assert.Equal(new string(Bullet, "sk-secret".Length), view.VisibleText);
        Assert.Equal("sk-secret", view.Text.ToString());
        Assert.Equal("sk-secret", value.Value);
    }

    [Fact]
    public void VisibleTextIsTheRealTextInAnOrdinaryField()
    {
        var value = new State<string>("sk-secret");
        using var h = Field(value, masked: false, new FakeClipboard(), out var view);

        Assert.Equal("sk-secret", view.VisibleText);
    }

    [Fact]
    public void TheUiSnapshotNeverCarriesAFieldsValue()
    {
        var value = new State<string>("sk-secret");
        using var h = Field(value, masked: true, new FakeClipboard(), out _);
        h.Render();

        // Snapshots (and so the MCP server's gui_snapshot) read text off TextViews only, and a text
        // field is not one — so no field's contents, masked or otherwise, reach an LLM transcript.
        Assert.DoesNotContain("sk-secret", h.Snapshot().ToText());
    }

    // ---- the substitute glyph is a real glyph ----

    [Fact]
    public void TheMaskGlyphRendersInTheBundledFont()
    {
        var fonts = new FreeTypeFontBackend();
        try
        {
            var font = fonts.LoadFontFromFile(
                Path.Combine(AppContext.BaseDirectory, "Assets", "Inter-Regular.ttf"), 32);

            Span<ShapedGlyph> shaped = stackalloc ShapedGlyph[4];
            var count = fonts.ShapeText(font, TextInputView.MaskCharacter.ToString(), shaped);
            Assert.Equal(1, count);

            // Glyph 0 is .notdef — what a font hands back for a codepoint it doesn't carry, and what
            // a mask picked by assumption would quietly draw for every character of the secret.
            Assert.NotEqual(0u, shaped[0].GlyphIndex);
            Assert.True(shaped[0].XAdvance > 0f, "the mask glyph must advance the pen");

            Assert.True(fonts.TryGetGlyph(font, shaped[0].GlyphIndex, out var glyph));
            Assert.True(glyph.Width > 0 && glyph.Height > 0,
                $"the mask glyph rasterized to {glyph.Width}x{glyph.Height}");
            Assert.True(HasInk(fonts, glyph), "the mask glyph rasterized to an empty bitmap");
        }
        finally
        {
            fonts.Dispose();
        }
    }

    private static bool HasInk(FreeTypeFontBackend fonts, in GlyphRenderInfo glyph)
    {
        var pixels = fonts.AtlasPixels;
        for (var y = 0; y < glyph.Height; y++)
        for (var x = 0; x < glyph.Width; x++)
            if (pixels[(glyph.AtlasY + y) * fonts.AtlasWidth + glyph.AtlasX + x] != 0)
                return true;
        return false;
    }

    private static void Drag(GuiTestHarness h, TextInputView view, float fromX, float toX)
    {
        // The first visual line is the 16px band under the field's top edge.
        var y = view.Position.Top - 8f;
        h.MoveTo(view.Position.Left + fromX, y);
        h.Press();
        h.MoveTo(view.Position.Left + toX, y);
        h.Release();
    }
}
