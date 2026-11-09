# ZGF.Gui.Testing

Headless harness for driving and inspecting a `ZGF.Gui` view tree without a window. Built so
an **LLM can debug a GUI as text**: it reads the screen as a snapshot, acts by intent, and sees what
changed — no pixels required.

## The loop

```
Create → Settle → Snapshot → act by label → Diff
```

```csharp
using var h = GuiTestHarness.Create(ctx => new MyScreen().BuildView(ctx));
h.Settle();                              // run animations to rest so the frame is stable
var before = h.Snapshot();               // the screen, as text
h.Click("Stage All");                    // act by label, not coordinates
h.Settle();
Console.Write(before.DiffTo(h.Snapshot()));
```

## Snapshot — the screen as text

`harness.Snapshot()` returns a `UiSnapshot`. `ToText()` renders the laid-out tree, one node per line:

```
FlexView #root [0,0 800x600]
  TextView "Local Changes" [16,12 180x20]
  KbmInput #stage-all role=button "Stage All" [16,40 120x32] hovered
  Column #file-list [16,80 768x400] clip
    Row role=listitem "src/App.cs" [16,80 768x28] selected
    Row role=listitem "src/Program.cs" [16,108 768x28]
  (Spinner #busy hidden)
```

- Coordinates are `[left,bottom WxH]`, rounded to ints (diff-stable).
- `role=` / `"label"` / states (`selected`, `focused`, `hovered`, `disabled`, …) come from each
  view's `Accessibility` plus live focus/hover from the input system.
- Hidden subtrees collapse to a single `(Type #id hidden)` marker.
- `ToJson()` gives the same tree for scripting.

## Acting by intent

- `Click("Push")` — clicks the button whose accessible label matches. A miss throws with the nearest
  labels and the full snapshot attached, so a wrong label self-corrects.
- `Get("search-box")` — resolves a view by id or label.
- `Type("hello")`, `PressKey(...)`, `ClickOn(view|id)`, `MoveTo`, `Scroll`, …
- `Settle()` ticks the clock until no animation is running (or a cap elapses).

## Asserting (intent-level, snapshot-attached failures)

```csharp
h.AssertVisibleText("Local Changes");
h.AssertNotVisible("Spinner");
h.AssertFocused("search-box");
h.AssertState("src/App.cs", AccessibilityStates.Selected);
```

## Screenshots — for genuinely visual bugs

The text snapshot is the workhorse; a real PNG complements it for overlap/colour/clipping bugs.
`CreateGpu` measures and draws the tree with the shipping canvas on a windowless `GpuDevice`
(`ZGF.Gui.Testing.Gpu`), so layout uses real font metrics and the pixels are what a window shows.

```csharp
using var device = GpuDevice.Create();   // one OpenGL device per process; share it across tests
using var h = GuiTestHarness.CreateGpu(device, ctx => new MyScreen().BuildView(ctx), 800, 600);
h.Settle();
h.SaveScreenshot("scratch/changes.png");   // straight-alpha PNG
RgbaImage pixels = h.Screenshot();          // premultiplied RGBA, rows top-down, for assertions
```

The harness runs everything that touches the tree on the device's render thread; to change a view
yourself, do it inside `device.Run(...)`. Only the main tree is drawn; open context menus are
separate windows. `Create` uses fast synthetic metrics for layout unit tests and has no screenshot.

To assert what the production canvas stages without a GPU — instances, clips, fonts, allocations —
draw onto a `CaptureCanvas`.

## Making widgets snapshot-friendly

Snapshots improve as widgets declare semantics. Most don't need to: `KbmInput` with a click handler
is already `role=button`, and `TextView` is `role=text`. For custom rows/controls:

```csharp
// static role:
myRowWidget.WithRole(AccessibilityRole.ListItem)
// live state (auto-tracked):
           .WithAccessibleStates(() => selected ? AccessibilityStates.Selected : AccessibilityStates.None)
```

Unannotated views fall back to their type name plus aggregated child text, so the snapshot is useful
from day one.
