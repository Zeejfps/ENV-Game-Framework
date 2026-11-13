# Immediate-mode widgets: behavior and painters

An immediate-mode widget does two separate jobs:

- **Behavior.** It finds out what happened from last frame's layout and this frame's input: hover,
  press, click, focus, keyboard activation, dragging. It also takes the cursor, the Tab stop and
  the keys it uses.
- **Painting.** It records the widget's element and everything drawn in it.

For buttons, scroll bars and tabs these two jobs are split, and a dock space's group headers are
assembled by a painter from parts that each bring their own behavior. The behavior returns a state value that
describes what to draw. A painter takes that state and records the look. An app can change the
look without changing the behavior, in two ways.

## 1. A theme painter restyles a stock widget everywhere

`UiTheme` holds an optional painter for each split widget:

| Property | Draws | Stock look |
|---|---|---|
| `ButtonPainter` | every `ui.Button`, including dialog and other built-in buttons | `ui.PaintButton(state, face)` |
| `IconButtonPainter` | every `ui.IconButton` | `ui.PaintIconButton(state, face)` |
| `ScrollBarPainter` | the bars of every `ui.Scroll` and `ui.VirtualScroll` | `ui.PaintScrollBar(bar)` |
| `TabPainter` | every tab of a `ui.TabStrip`, a dock space's included | `ui.PaintTab(state, face)` |

A null painter draws the stock look. The stock painters are public, so an app's painter can restyle
some cases and pass the rest on to the stock look. The theme is part of the app's `IUiState`, so
the painter reaches the widgets through the app's own state. Nothing is registered globally.

```csharp
state.Theme = UiTheme.Dark with
{
    ButtonPainter = PillButton,
    ScrollBarPainter = SlimScrollBar,
    ScrollBarWidth = 4f,          // gutters, grab areas and thumb lengths still come from these metrics
};

static void PillButton(Ui ui, in ButtonState state, in ButtonFace face)
{
    if (face.Variant != ButtonVariant.Primary)
    {
        ui.PaintButton(state, face);  // keep the stock look for the rest
        return;
    }
    var fill = state.Down ? Cyan.Pressed : state.Hovered ? Cyan.Hovered : Cyan.Rest;
    using (ui.Row(state.Id, new Box { Width = face.Width ?? Sizing.Fit(), Height = 40f, Padding = new Spacing(20f, 0f),
               AlignX = Align.Center, Background = fill, Radius = 20f }))
    {
        ui.Text(face.Label, color: Ink);
        if (state.FocusVisible) ui.FocusRing(20f);
    }
}
```

A button painter **must** record its element with `state.Id`. That id is what the behavior
hit-tests on the next frame. Size, padding and shape belong to the painter.

A scroll-bar painter is called while the bar's grab area is being recorded. The grab area is a
floating element over the thumb, and its size comes from the theme's metrics. The painter does its
drawing in a `ui.Paint` call. `bar.ThumbRect(p.Rect)` and `bar.TrackRect(p.Rect)` give the thumb
and its lane in UI space. `bar.Hovered`, `bar.Dragged`, `bar.PointerWithin`, `bar.Scrolled` and
`bar.SinceMoved` tell it what state the bar is in:

```csharp
static void SlimScrollBar(Ui ui, in ScrollBarState bar)
{
    const double linger = 0.8;
    var active = bar.Dragged || bar.PointerWithin || bar.SinceMoved < linger;
    if (bar.SinceMoved < linger) ui.RequestUpdateIn(linger - bar.SinceMoved);
    var shown = ui.Animate(bar.Id, active ? 1f : 0f);
    ui.Paint((bar, shown), static (p, s) =>
    {
        var thumb = s.bar.ThumbRect(p.Rect);
        p.FillRect(thumb, UiColor.Mix(0x0017CDFF, 0xFF17CDFF, s.shown), thumb.Width / 2);
    });
}
```

Dragging, the wheel, gutters and `ScrollTo` work the same with any painter.

## 2. Behavior-only, for a widget the app paints itself

For a one-off widget, call the behavior and paint the widget yourself:

```csharp
var back = ui.ButtonBehavior("back");          // hand cursor, Tab stop, Enter/Space, click
using (ui.Row(back.Id, new Box { Width = Sizing.Fit(), Height = 36f, Padding = new Spacing(12f, 0f), Radius = 18f,
           Background = back.Hovered ? HoverFill : 0u }))
{
    ui.Glyph(ArrowLeft, Cyan.Rest, 16f, new Box { Width = 16f, Height = 16f });
    ui.Text("Back", color: Cyan.Rest);
}
if (back.Activated) GoBack();
```

`ButtonState.Availability` is `Enabled`, `Disabled` or `Busy`. The pointer and focus fields are
false unless it is `Enabled`. `Down` is true while the pointer holds the button, and on the frame a
key activates it.

A tab painter records the tab with `state.Id`. When `state.Close` is not null the tab can close, and
the painter records that button with its id too, wherever it puts it; `ui.PaintCloseButton(close)`
draws the stock one. The close button is split the same way on its own: `ui.CloseButtonBehavior(id)`
and `ui.PaintCloseButton(state)`.

## 3. A dock header painter assembles a group's header from parts

A dock group's header is not one widget but a row of parts, so `ui.DockSpace(..., header: painter)`
takes a `DockHeaderPainter(Ui ui, in DockHeader header)` instead of a theme painter (the theme lives
below the dock space). The painter lays the header out and calls the parts it shows, where it shows
them; each call does that part's behavior, and the painter records its element with the id given:

| Part | Behavior |
|---|---|
| `header.Tabs(box)` | the group's tab strip: pick, close, drag, context menu, drop between tabs |
| `header.Grip(id)` | drag the front panel (Ctrl floats it), right-click for its menu; no focus |
| `header.MenuButton(id, label)` | a button opening the front panel's menu below it (`DockSpace.TabMenu`) |
| `header.CloseButton(id)` | a button closing the front panel; null when it can't close |

A part the painter does not call takes no input: no Tab stop, no drag. That is why the parts are
calls rather than fields of a state computed up front. `header.IdOf("part")` gives ids distinct per
group; `header.Panel`, `header.Tab`, `header.PanelCount` and `header.Floating` say what the header is
for. The stock header, `ui.PaintDockHeader(header)`, is `header.Tabs()` alone.

```csharp
static void PaneHeader(Ui ui, in DockHeader header)
{
    using (ui.Row(new Box { Width = Sizing.Grow(), Height = 32f, AlignY = Align.Center }))
    {
        var grip = header.Grip(header.IdOf("grip"));
        ui.Leaf(grip.Id, new Box { Width = 12f, Height = 20f });   // paint dots in it
        if (header.PanelCount > 1)
            header.Tabs(new Box { Width = Sizing.Grow() });
        else
            ui.Text(header.Tab.Title, new Box { Width = Sizing.Grow() });
        var menu = header.MenuButton(header.IdOf("menu"), "Panel menu");
        ui.PaintIconButton(menu, new IconButtonFace(More));
        var close = header.CloseButton(header.IdOf("close"));
        if (close != null)
            ui.PaintCloseButton(close.Value);
    }
}
```

## Splitting the next widget

Follow the button:

1. Move what the widget *does* into a behavior call, `ui.XBehavior(id, …)`, that returns an
   `XState` record struct. Use an enum, not a set of flags, for states that exclude each other.
2. Put the content the caller passes, such as the label or the value, in an `XFace` record struct.
3. Declare `delegate void XPainter(Ui ui, in XState state, in XFace face)` next to `UiTheme`, and
   add a nullable `XPainter` property to the theme.
4. Move today's painting, unchanged, into a public `ui.PaintX(state, face)`. The widget calls the
   theme's painter if there is one, and `PaintX` otherwise.
5. Pin the stock look before you move anything. `StockLookTests` dumps every draw call, captured
   from the code before the split. Extend that test, and check that a static painter allocates
   nothing per frame (`PainterAllocationTests`).

Use static methods or static lambdas as painters, and pass what a `ui.Paint` callback needs as its
state, so a frame allocates nothing.
