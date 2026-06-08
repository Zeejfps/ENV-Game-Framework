# W2 — Layout-only invalidation (delete the `SetDirty()` convention)

Parent: [V2-Redesign.md](./V2-Redesign.md) workstream W2. Builds on W1's split invalidation
(`InvalidateMeasure`/`InvalidateArrange`); read [W1-Layout.md](./W1-Layout.md) first.

## Scope decision: continuous redraw stays; we only optimize layout CPU

The framework redraws every frame and that is **intentional and correct**. Change-detection
for the GPU is the renderer's job, and `RenderedCanvasBase` already does it: every frame it
stages primitives, sorts, and `UploadIfChanged()` — re-uploading to the GPU only when the
batch actually changed. Re-issuing draws for an unchanged frame is cheap and inherently
per-frame. Making the framework dirty-gate *presentation* (retained draw tree, "only redraw
when something changed") is complexity we deliberately do **not** take on.

The avoidable cost is **layout CPU** — the measure/arrange tree walk — and W1 already gates
that so only dirty subtrees recompute. W2 finishes the job: make sure layout invalidates
*precisely* (and only) when geometry actually changes, and stop paying layout for changes
that don't.

### Consequence: appearance changes need no invalidation at all

If every frame redraws, an **appearance-only** change (colour, highlight, theme swap) is
visible on the next frame for free — draw reads the field live. So the current `SetDirty()`
on a colour change isn't just boilerplate, it's **actively wasteful**: `SetDirty()` sets
`IsSelfDirty` (`View.cs:407-410`), forcing an `OnLayoutSelf`/`OnLayoutChildren` pass for a
change that moves nothing.

```csharp
// GitBench CommitsView / BranchesView / LocalChangesPanel today:
this.BindThemed(s => { _rowText.TextColor = s.RowText; /*...*/ SetDirty(); });  // needless layout
selection.Subscribe(_ => SetDirty());                                           // needless layout
```

Both `SetDirty()` calls are **deleted** in V2 — the colour/selection update lands next frame,
and we stop paying for a layout pass that changes no geometry.

## The whole of W2

> **Layout-affecting writes invalidate layout (W1's `Measure`/`Arrange` tiers). Everything
> else is a plain field assignment with no invalidation.** There is no paint tier, no redraw
> pump, no dirty-gated present. The win is removing needless layout work and the `SetDirty()`
> convention, not changing how rendering is driven.

## Typed setters replace the single `SetField`

V1's one `SetField → SetDirty` (`View.cs:397-410`) becomes two layout setters; appearance
properties drop the helper entirely.

```csharp
protected bool SetMeasure<T>(ref T field, T value)  // size-affecting
{ if (Eq(field, value)) return false; field = value; InvalidateMeasure(); return true; }

protected bool SetArrange<T>(ref T field, T value)  // position-affecting, sizes unchanged
{ if (Eq(field, value)) return false; field = value; InvalidateArrange(); return true; }

// Appearance-only: just a plain auto-property. No setter logic, no invalidation —
// the next frame draws the new value.
public uint TextColor { get; set; }
```

Reclassification (representative; full sweep during migration):

| Property | Setter | Why |
| --- | --- | --- |
| `TextView.TextColor`, `Rotation`, alignment, `TextOverflow` | plain (none) | appearance within an unchanged box; redrawn next frame |
| `RectView.BackgroundColor`/`BorderColor`/`BorderRadius`/`BorderSize` | plain (none) | drawn, not laid out |
| `View.ZIndex` | plain (none) | affects draw order + hit-test, read live; no cached geometry |
| `TextView.Text`, `FontSize`, `FontFamily`, `FontWeight`, `TextWrap` | `SetMeasure` | changes text metrics / wrapping |
| `View.IsVisible` | `SetMeasure` | skipped from layout (gap/size change) |
| `View.Width`/`Height`/`Min*`/`Max*` | `SetMeasure` | preferred size (W1) |
| `FlexView.Gap`, `PaddingView.Padding`, `CenterView.Margin` | `SetMeasure` | change extent / child sizing |
| `FlexView.MainAxisAlignment` | `SetArrange` | repositions, sizes unchanged |
| `FlexView.CrossAxisAlignment` | `SetMeasure` | `Stretch` changes child cross size |
| `View.Id` | plain (none) | no visual effect |

The rule of thumb: **does it change geometry?** Yes → `SetMeasure`/`SetArrange`. No → plain
property. When unsure, a plain property is the cheap default (worst case: a stale frame that
never happens because we redraw every frame); a wrong `SetMeasure` only costs needless layout.

## Uniform continuous redraw (closes the popup gap)

Today the main window redraws every frame (`OpenGlApp.cs:134`) but popups/secondary windows
are dirty-gated — `OpenGlApp.cs:139` skips windows with `!NeedsRedraw`, and they only
`RequestRedraw()` from `OnAnyInput` (`SecondaryWindowFactory.cs:132`, `PopupWindowFactory.cs:295`).
Under "appearance change = no invalidation," a dirty-gated popup would miss async/animated
content changes (a spinner in a clone dialog, a theme swap) until the mouse moves.

V2 makes **every visible window redraw each frame**, like the main window — drop the
`NeedsRedraw` gate:

```diff
  for (var i = 0; i < _windows.Count; i++) {
      if (!w.IsVisible) continue;
-     if (!w.NeedsRedraw) continue;
      w.MakeContextCurrent();
      w.RenderNow();
  }
```

Popups already `SwapInterval(0)` so they don't gate vsync; `NeedsRedraw` was an optimization,
not a correctness requirement. This deletes the special-casing and the latent repaint bug at
once, and keeps one uniform model: all windows redraw continuously, all of them rely on the
renderer's `UploadIfChanged` for GPU efficiency. Spinners and async content just work — no
animation pump needed.

## Custom-drawn views (where the `SetDirty()` calls live)

`CommitsView`/`BranchesView` draw rows directly in `OnDrawSelf` from cached `TextStyle`
fields — no per-row `TextView` to bind. Their `BindThemed(...; SetDirty())` is the real
concentration of manual invalidation, and it all goes away:

```csharp
// appearance theme reaction — drop SetDirty entirely; redrawn next frame
this.BindThemed(s => { _rowText.TextColor = s.RowText; /*...*/ });

// the rare theme reaction that changes metrics (font size) still needs layout:
this.BindThemed(s => { _rowText.FontSize = s.RowFontSize; InvalidateMeasure(); });
```

`BindThemed` does not need a tier parameter: appearance is the default (do nothing extra),
and the unusual metrics case calls `InvalidateMeasure()` in the body. Standalone
`Subscribe(_ => SetDirty())` for selection/hover highlight is simply deleted.

## Binding integration

The binding behaviors (`PropertyBindingBehavior`, `DerivedPropertyBindingBehavior`,
`ThemedDerivedPropertyBindingBehavior`, `Bindings/BindingExtensions.cs`) assign via a setter
lambda. They inherit correctness from the target property: binding a `SetMeasure` property
invalidates layout; binding a plain appearance property invalidates nothing. No binding calls
`SetDirty()`; call sites (`BindText`, `BindBackgroundColor`, …) need **no change** — only the
target property's classification.

## Rejected: wrapping every property in an observable

Still rejected. A `ViewProp<T>`/`State<T>` per property adds an allocation + indirection to
every property of every view — taxing the virtualized commit/diff lists W1 speeds up. View
properties are *sinks*; the reactive sources live in the VM. Plain C# properties for the sink
case; keep `State<T>` exposed by a view only for genuine **two-way** control state
(`CheckboxView.IsChecked`), as today.

## Phasing

1. **Split `SetField` into `SetMeasure`/`SetArrange`;** demote appearance properties to plain
   auto-properties; reclassify per the table. Pure cleanup — continuous redraw already covers
   appearance, so nothing regresses visually, and needless layout passes drop.
2. **Delete app-side `SetDirty()`** on appearance/theme/selection reactions; `BindThemed`
   bodies that only set colours lose their trailing `SetDirty()`.
3. **Uniform continuous redraw:** drop the `NeedsRedraw` gate for secondary/popup windows.
   Verify spinners/async dialogs repaint without input.
4. **Delete `SetDirty()`** from the `View` surface once no caller remains.

## Open questions / deferred

- **Idle power.** Continuous redraw means a static screen still pins the GPU at vsync. We are
  *deferring* this by decision — it's the renderer's domain, not the view framework's, and
  `UploadIfChanged` already removes the GPU upload. If it ever becomes a product concern
  (battery), a future optional dirty-gated present mode could live behind a flag in the
  *renderer/windowing* layer, leaving the view-side model (this doc) untouched. Explicitly
  out of scope for V2.
- **`MainAxisAlignment` as `SetArrange`.** Confirm alignment never changes a child's measured
  size (it doesn't — it only distributes free space), so `SetArrange` (not `SetMeasure`) is
  correct and skips re-measure.
