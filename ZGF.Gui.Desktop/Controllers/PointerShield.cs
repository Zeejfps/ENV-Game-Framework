using ZGF.Gui.Desktop.Input;

namespace ZGF.Gui.Desktop.Controllers;

/// <summary>
/// Makes a view opaque to the pointer without giving it any behaviour of its own.
///
/// <para>Hit-testing only ranks views that have a controller registered, so the pixels of a floating panel
/// that carry no interactive child — a popover's padding, the gutters a grid leaves between its cells — are
/// invisible to it, and whatever sits <em>underneath</em> the panel wins hover there. That is how a date
/// picker floated over a chart could light the chart up as the cursor crossed the space between two day
/// cells. A popover is pointer-modal within its own rect; this is the one line that says so.</para>
///
/// <para>It only claims the hover, it does not consume the event: a shielded view still dispatches to its
/// own ancestors exactly as its interactive children do, so the panel behaves the same wherever inside it
/// the pointer happens to land. Descendants keep winning over the shield — they sit in front of it at the
/// same accumulated z (see <see cref="View.DrawZIndex"/>) — so day cells, buttons and fields are unaffected.</para>
/// </summary>
public sealed class PointerShieldController : KeyboardMouseController;

public static class PointerShieldExtensions
{
    /// <summary>Registers a <see cref="PointerShieldController"/> for the view's mounted lifetime. Apply it to
    /// the root of anything that floats above other content, so the parts of it that are merely painted still
    /// stop the pointer.</summary>
    public static void UsePointerShield(this View view, InputSystem input) =>
        view.UseController(input, () => new PointerShieldController());
}
