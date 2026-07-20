using ZGF.Geometry;
using ZGF.Gui.Desktop.Input;

namespace ZGF.Gui.Tests;

/// <summary>
/// Which window the OS IME composes against is not the window the editing field lives in. A
/// searchable context menu's search box lives in a popup, but a borderless popup never takes OS
/// keyboard focus on Windows — so the keys, and therefore the composition, belong to the host
/// window. Enable the IME on the popup instead and the box takes Latin but not CJK.
///
/// <para>The coordinator derives the answer from focus every tick rather than letting fields toggle
/// it, which is what makes closing a menu over a still-editing commit box safe: nobody has to
/// remember to hand the IME back. That regression is pinned below as a test.</para>
///
/// <para>The routing is invisible to <see cref="ZGF.Gui.Testing.GuiTestHarness"/> — it is
/// single-window and injects compositions straight into one InputSystem — so it is tested here
/// against fake windows.</para>
/// </summary>
public class ImeCoordinatorTests
{
    /// <summary>A window as the coordinator sees it: OS focus, keyboard focus, the native IME
    /// switches, and a canvas→screen mapping that offsets by the window's screen origin — enough to
    /// tell which window's canvas a caret rect was converted through.</summary>
    private sealed class FakeWindow(int originX = 0, int originY = 0) : IImeWindow
    {
        public bool OsFocused { get; set; }
        public bool HasKeyboardFocus { get; set; }
        public bool ImeEnabled { get; private set; }
        public int FocusTransitions { get; private set; }
        public RectI? CursorRect { get; private set; }
        public int Resets { get; private set; }

        public bool IsWindowFocused() => OsFocused;
        public bool IsCursorInsideWindow() => false;

        public void SetTextInputFocus(bool focused)
        {
            ImeEnabled = focused;
            FocusTransitions++;
        }
        public void SetImeCursorRect(RectI screenRect) => CursorRect = screenRect;
        public void ResetImeComposition() => Resets++;

        public RectI CanvasToScreen(RectF r) =>
            new((int)r.Left + originX, (int)r.Bottom + originY, (int)r.Width, (int)r.Height);
    }

    private static readonly RectF HostCaret = new(10, 20, 2, 16);
    private static readonly RectF MenuCaret = new(30, 40, 2, 16);

    /// <summary>The host window plus an open searchable menu: a popup that is modal, holds keyboard
    /// focus, and whose search box is editing. The host keeps OS focus throughout — that is the
    /// whole problem.</summary>
    private static (ImeCoordinator Ime, PointerOwnershipArbiter Arbiter, FakeWindow Host, FakeWindow Menu) SearchableMenu()
    {
        var arbiter = new PointerOwnershipArbiter();
        var ime = new ImeCoordinator(arbiter);

        var host = new FakeWindow { OsFocused = true };
        var menu = new FakeWindow(originX: 100, originY: 200) { HasKeyboardFocus = true };

        arbiter.Register(host, isModal: false);
        arbiter.Register(menu, isModal: true);
        ime.Register(host);
        ime.Register(menu);

        ime.SetFieldEditing(menu, true);
        ime.SetFieldCaret(menu, MenuCaret);
        return (ime, arbiter, host, menu);
    }

    [Fact]
    public void PopupFieldEditing_ComposesAgainstTheHostWindow()
    {
        var (ime, _, host, menu) = SearchableMenu();

        ime.Update();

        Assert.True(host.ImeEnabled);
        Assert.False(menu.ImeEnabled);
    }

    /// <summary>The caret belongs to the menu's field, so it converts through the menu's canvas —
    /// but it is pushed to the window that is actually composing.</summary>
    [Fact]
    public void PopupFieldEditing_AnchorsTheCandidateWindowOnTheMenusCaret()
    {
        var (ime, _, host, menu) = SearchableMenu();

        ime.Update();

        Assert.Equal(menu.CanvasToScreen(MenuCaret), host.CursorRect);
        Assert.Null(menu.CursorRect);
    }

    /// <summary>The regression the old "just enable the IME on every window" fix would have caused,
    /// and the reason the editing set is a dictionary rather than a single slot: with a commit box
    /// and a menu's search box both editing, the menu closing must hand the IME back rather than
    /// switch it off under the field that still has it.</summary>
    [Fact]
    public void MenuClosing_LeavesTheImeOnTheStillEditingHostField()
    {
        var (ime, arbiter, host, menu) = SearchableMenu();
        ime.SetFieldEditing(host, true);
        ime.SetFieldCaret(host, HostCaret);
        ime.Update();
        Assert.Equal(menu.CanvasToScreen(MenuCaret), host.CursorRect);

        // The menu closes: its field ends its edit session, and the popup leaves the arbiter.
        ime.SetFieldEditing(menu, false);
        menu.HasKeyboardFocus = false;
        arbiter.Unregister(menu);
        ime.Update();

        Assert.True(host.ImeEnabled);
        Assert.Equal(host.CanvasToScreen(HostCaret), host.CursorRect);
    }

    /// <summary>A plain (non-searchable) menu focuses nothing, so typing — and composing — stay with
    /// the host window's own field rather than being handed to the menu.</summary>
    [Fact]
    public void MenuWithoutKeyboardFocus_LeavesComposingWithTheHostField()
    {
        var (ime, _, host, menu) = SearchableMenu();
        ime.SetFieldEditing(menu, false);
        menu.HasKeyboardFocus = false;
        ime.SetFieldEditing(host, true);
        ime.SetFieldCaret(host, HostCaret);

        ime.Update();

        Assert.True(host.ImeEnabled);
        Assert.Equal(host.CanvasToScreen(HostCaret), host.CursorRect);
    }

    /// <summary>macOS: a borderless popup does take key status, so the popup is the focused window
    /// and composes for itself. The same derivation covers it — no platform branch.</summary>
    [Fact]
    public void PopupHoldingOsFocus_ComposesAgainstItself()
    {
        var (ime, _, host, menu) = SearchableMenu();
        host.OsFocused = false;
        menu.OsFocused = true;

        ime.Update();

        Assert.True(menu.ImeEnabled);
        Assert.False(host.ImeEnabled);
        Assert.Equal(menu.CanvasToScreen(MenuCaret), menu.CursorRect);
    }

    /// <summary>No field editing: the IME is off, or a CJK layout would start composing on the keys
    /// that navigate the commit list.</summary>
    [Fact]
    public void NothingEditing_LeavesTheImeOff()
    {
        var (ime, _, host, menu) = SearchableMenu();
        ime.SetFieldEditing(menu, false);

        ime.Update();

        Assert.False(host.ImeEnabled);
        Assert.False(menu.ImeEnabled);
    }

    [Fact]
    public void FieldEndingItsSession_TurnsTheImeBackOff()
    {
        var (ime, _, host, menu) = SearchableMenu();
        ime.Update();
        Assert.True(host.ImeEnabled);

        ime.SetFieldEditing(menu, false);
        ime.Update();

        Assert.False(host.ImeEnabled);
    }

    /// <summary>The app in the background composes nothing, however many of its fields are still
    /// editing — the keys are going to another application.</summary>
    [Fact]
    public void AppLosingFocus_TurnsTheImeOff()
    {
        var (ime, _, host, menu) = SearchableMenu();
        ime.Update();
        Assert.True(host.ImeEnabled);

        host.OsFocused = false;
        ime.Update();

        Assert.False(host.ImeEnabled);
        Assert.False(menu.ImeEnabled);
    }

    /// <summary>A window that goes away while composing takes the composition with it — a later tick
    /// must not keep pushing a caret at a disposed window.</summary>
    [Fact]
    public void UnregisteringTheComposingWindow_DropsIt()
    {
        var (ime, _, host, menu) = SearchableMenu();
        ime.Update();
        Assert.True(host.ImeEnabled);

        ime.Unregister(host);
        ime.Update();

        Assert.False(menu.ImeEnabled);
    }

    /// <summary>Handing composition from one window to another must clear the old one, not merely
    /// set the new one. Only ever forwarding the "on" direction — which is what this seam used to do —
    /// would leave both windows claiming text-input focus, and on Windows that means two associated
    /// IME contexts, one of them on a window receiving no keys.</summary>
    [Fact]
    public void CompositionMovingBetweenWindows_LeavesOnlyTheNewOneFocused()
    {
        var (ime, _, host, menu) = SearchableMenu();
        ime.Update();
        Assert.True(host.ImeEnabled);

        // The popup takes OS focus (macOS behaviour), so composition moves host -> menu.
        host.OsFocused = false;
        menu.OsFocused = true;
        ime.Update();

        Assert.False(host.ImeEnabled);
        Assert.True(menu.ImeEnabled);
    }

    /// <summary>Text-input focus is a native call per transition, and on Windows it associates and
    /// disassociates the window's IME context. Re-asserting it every tick would churn that context
    /// under a live composition, so an unchanged state must produce no call at all.</summary>
    [Fact]
    public void SteadyState_DoesNotRepeatTheNativeFocusCall()
    {
        var (ime, _, host, _) = SearchableMenu();

        ime.Update();
        var afterFirst = host.FocusTransitions;
        ime.Update();
        ime.Update();

        Assert.Equal(1, afterFirst);
        Assert.Equal(1, host.FocusTransitions);
    }

    /// <summary>The composition lives on the window that is composing, so that is where a blur or an
    /// Escape has to go — not the window whose field asked.</summary>
    [Fact]
    public void ResetComposition_ResetsTheComposingWindowNotTheFieldsOwn()
    {
        var (ime, _, host, menu) = SearchableMenu();
        ime.Update();

        ime.ResetComposition();

        Assert.Equal(1, host.Resets);
        Assert.Equal(0, menu.Resets);
    }
}
