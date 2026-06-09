# W1 — Layout engine rewrite (concrete design)

Parent: [V2-Redesign.md](./V2-Redesign.md) workstream W1. This is the detailed design for
the layout core. It must support every layout V1 ships today; the mapping table at the end
proves each case is expressible.

## Problems being fixed (recap, with V1 evidence)

1. **Measure/arrange conflated; children measured 2–3× per pass.** `OnLayoutSelf` sizes and
   positions in one call (`View.cs:412-459`); `FlexColumnView.OnLayoutChildren` measures
   each child for total height (`:59`) then again after grow distribution (`:135`);
   `TextView` re-wraps again in `OnDrawSelf` (`:166`).
2. **Dirty doesn't propagate; reading it walks the subtree.**
   `IsChildrenDirty => _children.Any(c => c.IsDirty)` (`View.cs:182`), O(subtree) every
   access; `SetDirty()` sets a local flag only (`:407-410`).
3. **Constraints are mutable child fields the parent writes.** Parents assign
   `child.LeftConstraint/WidthConstraint/...` mid-layout (`FlexColumnView.cs:146-149`),
   re-dirtying the child; precedence of `Width` vs `WidthConstraint` vs `Min/Max` is
   resolved implicitly in `OnLayoutSelf`; intrinsic size isn't queryable without layout.
4. **4× duplicated container algorithms** (FlexColumn/FlexRow/Column/Row).
5. **Y-up math + scroll reversal hacks** (`currentTop -= ...`; `1f - scrollOffset` in
   `ScrollPane.cs:92`, `VerticalScrollPane.cs:137`).

## Design lineage

This is the **box-constraints model** (constraints flow down, sizes flow up, parent sets
position) — the same proven shape used by Flutter's `BoxConstraints`/`RenderBox`. It is
specifically the model that resolves all four hard cases V1 hand-rolls: caps (loose
constraints), flex grow (tight constraints after a solve), scroll (unbounded main axis),
and height-for-width text (max width drives returned height). We adopt the model, not the
framework.

## Core types

```csharp
// Immutable. Min/Max on each axis. Replaces the Width/WidthConstraint/Min*/Max* field soup.
public readonly record struct Constraints(
    float MinWidth, float MaxWidth,
    float MinHeight, float MaxHeight)
{
    public static Constraints Tight(Size s) =>
        new(s.Width, s.Width, s.Height, s.Height);

    public static Constraints Loose(float maxW, float maxH) =>
        new(0, maxW, 0, maxH);

    public static readonly Constraints Unbounded =
        new(0, float.PositiveInfinity, 0, float.PositiveInfinity);

    public Constraints LooseMainUnbounded(Axis axis) => axis == Axis.Vertical
        ? this with { MinHeight = 0, MaxHeight = float.PositiveInfinity }
        : this with { MinWidth = 0,  MaxWidth = float.PositiveInfinity };

    // Clamp a desired size into the allowed box. The single point where sizing rules live.
    public Size Constrain(Size s) => new(
        Math.Clamp(s.Width,  MinWidth,  MaxWidth),
        Math.Clamp(s.Height, MinHeight, MaxHeight));

    public bool HasBoundedWidth  => !float.IsPositiveInfinity(MaxWidth);
    public bool HasBoundedHeight => !float.IsPositiveInfinity(MaxHeight);

    public bool HasBoundedMain (Axis a) => a == Axis.Vertical ? HasBoundedHeight : HasBoundedWidth;
    public bool HasBoundedCross(Axis a) => a == Axis.Vertical ? HasBoundedWidth  : HasBoundedHeight;
}

public enum Axis { Horizontal, Vertical }
```

Axis helpers (the key to deleting the 4× duplication):

```csharp
internal static class AxisExtensions
{
    public static float Main (this Axis a, Size s) => a == Axis.Vertical ? s.Height : s.Width;
    public static float Cross(this Axis a, Size s) => a == Axis.Vertical ? s.Width  : s.Height;
    public static Size  Pack (this Axis a, float main, float cross) =>
        a == Axis.Vertical ? new Size(cross, main) : new Size(main, cross);
}
```

## The protocol on `View`

Two phases, both driven top-down by the parent. Constraints are **arguments**; size is a
**return value**; position is assigned by the parent via `Arrange`.

```csharp
public abstract class View
{
    // ---- Measure: pure, idempotent, cached. Returns desired size within `c`. ----
    private Constraints _measuredFor;
    private Size _measuredSize;
    private bool _hasMeasured;

    public Size Measure(Constraints c)
    {
        if (_hasMeasured && !_needsMeasure && c.Equals(_measuredFor))
            return _measuredSize;                 // cache hit — generalizes TextView._wrappedForWidth

        _measuredSize = c.Constrain(MeasureContent(c));
        _measuredFor = c;
        _hasMeasured = true;
        _needsMeasure = false;
        return _measuredSize;
    }

    // Override per view. May call child.Measure(...) but must NOT position anything.
    protected abstract Size MeasureContent(Constraints c);

    // ---- Arrange: assign final geometry; recurse into children. ----
    public RectF Bounds { get; private set; }

    public void Arrange(RectF finalBounds)
    {
        Bounds = finalBounds;                     // Y-down: top-left origin
        ArrangeContent(finalBounds);
        _needsArrange = false;
    }

    // Override to place children. Children were already measured during MeasureContent;
    // a well-behaved container reuses those cached sizes (same Constraints => cache hit).
    protected virtual void ArrangeContent(RectF bounds) { }
}
```

### Why this kills the re-measure cascade

`Measure` caches on `Constraints`. When a flex container measures a child to compute totals
and then measures it again at the final width, the second call is a **cache hit** if the
constraints match. Height-for-width still works: the container measures the child with the
chosen width as a tight-width / unbounded-height constraint, the child returns its wrapped
height, and that same constraint recurs at arrange time → hit. `TextView`'s bespoke
`_wrappedForWidth`/`_wrappedFromText` cache (`TextView.cs:143`) disappears into the generic
one.

### Cache invariants (benchmark-verified — see `ZGF.Gui.Benchmarks/Layout*`)

The single-entry `(Constraints → Size)` cache only pays off if a node is measured with the
**same** constraint each pass. Four rules keep it that way; each was a real bug the
`LayoutBenchmarks`/`LayoutProbe` caught (steady-state was 41 ms / full re-shape before fixing):

1. **`Arrange` must not invalidate measure.** `Position` is a layout *output*; `Arrange`
   assigns the backing field directly. Routing it through `SetField` → `InvalidateMeasure`
   re-dirties every node and defeats the cache next frame.
2. **No `Tight` re-measure before `Arrange`.** `Arrange(rect)` already gives the child its
   final bounds and every `ArrangeContent` works off `bounds`, not the measured size. A
   `child.Measure(Tight(rect))` right before `child.Arrange(rect)` is a second, different
   constraint that thrashes the cache.
3. **Measure each child once per pass.** A non-stretch `FlexView` child needs its natural
   cross *and* its main; measure once cross-loose, reuse the result, and only re-measure in
   the rare cross-clamped case. (FlexView stores basis/cross in reused buffers.)
4. **A fully-tight node skips `MeasureContent`.** `View.Measure` returns the constraint size
   directly when `Min == Max` on both axes — its children are measured once in `Arrange`.
   Measuring them in `MeasureContent` too (often at a different cross constraint) double-shapes
   on every relayout.

Result on a 2000-row list: steady-state re-layout reshapes **0** nodes (full cache hit),
a one-row edit reshapes **1**, a forced root relayout reshapes **0**. Run `--layout` to verify.

## Sizing rule: `Width`/`Height` are *preferred*, never *imposed*

The single rule that removes V1's `Width` vs `WidthConstraint` vs `Min*`/`Max*` precedence
soup (`View.cs:414-451`):

> A view's `Width`/`Height` set a **preferred** size. The incoming `Constraints` always win
> via `Constrain`. A view can never choose a size outside the constraints it was given.

Encoded in one place — the base `Measure`, which already calls `c.Constrain(...)` on whatever
`MeasureContent` returns:

```csharp
protected override Size MeasureContent(Constraints c)
{
    // A leaf with a fixed size returns it as a PREFERENCE. If `c` is tight, Constrain in the
    // base Measure overrides it; if `c` is loose, the preference is honored (clamped to max).
    var preferred = new Size(Width ?? content.Width, Height ?? content.Height);
    return preferred;   // base Measure does c.Constrain(preferred)
}
```

This is what makes the grow + fixed-size case (below) resolve without a special rule:
under a **loose** constraint a fixed size is honored (it becomes the flex *basis*); under a
**tight** constraint from a parent that decided otherwise (a grow slot), the parent wins.

## Two invalidation kinds (not one `SetDirty`)

V1 has a single `SetDirty()`. We split it, because a scroll offset change must reposition
children **without** re-measuring anything.

```csharp
private bool _needsMeasure;            // self size may have changed
private bool _needsArrange;            // self/children need repositioning
private bool _descendantNeedsLayout;   // some node below needs measure or arrange

// Content/size-affecting change (text, font, child added, gap). Implies re-arrange.
protected void InvalidateMeasure()
{
    if (_needsMeasure) return;
    _needsMeasure = true;
    _needsArrange = true;
    BubbleDescendantFlag();
}

// Position-only change (scroll offset, alignment within an unchanged box).
protected void InvalidateArrange()
{
    if (_needsArrange) return;
    _needsArrange = true;
    BubbleDescendantFlag();
}

// O(depth-to-already-marked-ancestor), early-outs where the bit is already set.
private void BubbleDescendantFlag()
{
    for (var p = Parent; p != null && !p._descendantNeedsLayout; p = p.Parent)
        p._descendantNeedsLayout = true;
}
```

The root layout pass descends only where `_needsMeasure || _needsArrange ||
_descendantNeedsLayout` — **O(changed paths)**, replacing the per-node `Any()` subtree walk.
`SetField` routes to `InvalidateMeasure` by default; size-neutral paint props (W2) route to
a repaint flag instead and never touch layout.

`ScrollPane.Scroll()` becomes `InvalidateArrange()` only — content sizes are untouched, just
the child's `Y` offset changes.

## The single flex container

One class replaces `FlexColumnView` + `FlexRowView` + `ColumnView` + `RowView`. The V1
algorithm (`FlexColumnView.cs:40-168`) is preserved verbatim in structure — measure natural
main sizes, distribute remaining space by grow weight, shrink proportionally on overflow —
but written once against the axis abstraction.

```csharp
public sealed class FlexView : MultiChildView
{
    public Axis Axis { get; init; } = Axis.Vertical;
    public float Gap { get; set => SetMeasureField(ref field, value); }
    public MainAxisAlignment  MainAxisAlignment  { get; set => SetMeasureField(ref field, value); }
    public CrossAxisAlignment CrossAxisAlignment { get; set => SetMeasureField(ref field, value); }

    protected override Size MeasureContent(Constraints c)
    {
        var maxCross = Axis.Cross(new Size(c.MaxWidth, c.MaxHeight));
        var crossConstraint = /* Stretch => tight cross; else loose cross */;

        float mainSum = 0, crossMax = 0; int n = 0;
        foreach (var child in VisibleChildren)
        {
            // Loose on main so a grow child reports its NATURAL main size here.
            var size = child.Measure(crossConstraint.LooseMainUnbounded(Axis));
            mainSum += Axis.Main(size);
            crossMax = Math.Max(crossMax, Axis.Cross(size));
            n++;
        }
        var main = mainSum + Gap * Math.Max(0, n - 1);
        return Axis.Pack(main, crossMax);
    }

    protected override void ArrangeContent(RectF bounds) => ResolveFlex(bounds);  // algorithm below
}

// Back-compat-free aliases if call sites want names:
//   ColumnView  => new FlexView { Axis = Vertical }   (no grow children)
//   FlexRowView => new FlexView { Axis = Horizontal }
```

`FlexItem.Grow` stays as the per-child opt-in; a child not wrapped in a `FlexItem` has grow
0, exactly as today.

## Flex resolution: cross-first, basis, grow, shrink, clamp

This is the precise `ResolveFlex` ordering. Cross is resolved **before** main so that a
height-for-width child measures its main (height) at the width it will actually be laid out
at — exactly what V1 does with `finalChildWidth` then `MeasureHeight(finalChildWidth)`
(`FlexColumnView.cs:105-135`).

For each visible child, per axis (Vertical column shown; row is the axis mirror):

1. **Cross size first.** `Stretch` ⇒ cross = `bounds` cross (tight). Otherwise measure the
   child with a **cross-loose** constraint and use its returned cross (clamped to bounds
   cross). This fixes the child's width.
2. **Basis (natural main).** Measure the child with cross **tight** (the width from step 1)
   and main **loose-unbounded**. The returned main is the child's `basis`. For a fixed-size
   child this is its preferred main (honored because the constraint is loose); for wrapping
   text it's the height at the final width.
3. **Free space.** `free = mainExtent(bounds) − Σ basis − Gap·(n−1)`.
4. **Distribute.**
   - `free > 0 && Σgrow > 0`: each grow child gets `main = basis + (grow/Σgrow)·free`.
     Non-grow children keep `basis` — *this is why a non-grow fixed-size child stays fixed:
     no free space is ever handed to it.*
   - `free < 0` (overflow): shrink — distribute the negative `free` across **grow** children
     by grow weight, flooring at the child's min (and ≥ 0). Non-grow children keep `basis`
     and overflow (then clip/scroll). This matches V1 (`FlexColumnView.cs:140-144`), which
     overloads `Grow` for shrink rather than a separate `flex-shrink`.
5. **Clamp + freeze (min/max).** Clamp each flexed `main` to the child's preferred
   `Min*`/`Max*`. If a clamp changed a child's size, **freeze** it at the clamped value and
   re-run step 4 for the remaining unfrozen grow children with the leftover free space.
   Bounded by the child count (cheap). V1 did a single pass and could under/over-fill when a
   `Max` bit; the freeze loop is the intentional correctness upgrade.
6. **Arrange.** Place each child with a **tight** rect of (final cross, final main),
   advancing the main offset downward (Y-down): `mainOffset += finalMain + Gap`. The tight
   main is what makes grow override a child's preferred size — see the second worked case.

### Worked: grow + fixed size

`Grow` and fixed size on the **same** item — `new FlexItem { Grow = 1, Child = box }` where
`box.Height = 100`, slot has 250px free after other children:

- Step 2 basis: `box` measured loose-main → preferred `100` (loose, so honored).
- Step 4: grow child, `free = 250 − 100 = 150` all to it → `main = 100 + 150 = 250`.
- Step 6: `box.Arrange(tight height 250)`. Inside `box.Measure`, preferred `100` is
  `Constrain`ed by the tight `[250,250]` → `250`. **Grow wins; fixed height was the basis.**

Fixed size on the **inner** child, `Grow` on the wrapping `FlexItem`:

- The `FlexItem` has no preferred main of its own → its basis is the inner child's `100`.
- The item grows to `250`, then arranges the inner child with tight main `250`; the inner
  child's preferred `100` is `Constrain`ed up to `250`. **No leftover gap** — a fix over V1,
  where the inner child stayed `100` and left a `150` gap inside the grown item.

**If you want a hard fixed size grow can't override, don't put it in a `Grow` slot.** A
non-grow child never receives free space (step 4), so it stays at its basis. That is the
single, predictable rule; there is no separate "fixed wins over grow" mode.

### Worked: grow child with a `MaxHeight`

Two grow-1 children, `free = 300`, child A has `MaxHeight = 120`:

- First pass: each offered `+150` → A would be `basis_A + 150`. If that exceeds `120`, A is
  **frozen at 120** (step 5).
- Re-run step 4 for B only with the leftover free → B absorbs A's unused share instead of it
  being lost. (V1 single-pass would leave the slot under-filled.)

## Caps and centering become constraints, not field mutation

`CenterView` (`CenterView.cs:29-61`) currently mutates `child.MaxWidthConstraint` /
`MaxHeightConstraint` and hand-applies `Min*`. In the new model it just hands down a loose
constraint and centers the returned size:

```csharp
protected override void ArrangeContent(RectF bounds)
{
    var inner = Constraints.Loose(bounds.Width - Margin * 2, bounds.Height - Margin * 2);
    foreach (var child in VisibleChildren)
    {
        var size = child.Measure(inner);                       // capped by Loose max; floored by child's own min
        var x = bounds.X + (bounds.Width  - size.Width)  / 2f;
        var y = bounds.Y + (bounds.Height - size.Height) / 2f; // Y-down
        child.Arrange(new RectF(x, y, size.Width, size.Height));
    }
}
```

The "fixed `Width` ignores a plain width constraint, only `Max` reins it in" hack documented
in `CenterView.cs:37-39` evaporates: a child's fixed size is just `Constrain`ed by the
incoming `Loose` max in its own `Measure`.

## Scrolling: unbounded main axis + arrange-time translation

`ScrollPane`/`VerticalScrollPane` give content `max(viewport, natural)` and translate by a
scroll distance. In the new model the pane measures its content with an **unbounded main
axis** (so content reports full natural height), then arranges it offset by the scroll
distance, and clips at draw. Y-down removes the `1f - scrollOffset` inversion.

```csharp
protected override Size MeasureContent(Constraints c) =>
    // viewport takes the constraint's size; content is measured separately for scroll range
    new Size(c.MaxWidth, c.MaxHeight);

protected override void ArrangeContent(RectF bounds)
{
    var content = _content.Measure(bounds.WidthTight().HeightUnbounded());
    var contentHeight = Math.Max(bounds.Height, content.Height);
    _maxScroll = Math.Max(0, contentHeight - bounds.Height);
    _distanceFromTop = Math.Clamp(_distanceFromTop, 0, _maxScroll);

    // Y-down: positive scroll moves content UP, so subtract from Y.
    _content.Arrange(new RectF(bounds.X, bounds.Y - _distanceFromTop, bounds.Width, contentHeight));
    ScrollNormalized = _maxScroll == 0 ? 0 : _distanceFromTop / _maxScroll;  // no 1f - x
}

public void Scroll(float delta)
{
    _distanceFromTop = Math.Clamp(_distanceFromTop + delta, 0, _maxScroll);
    InvalidateArrange();   // <-- not InvalidateMeasure: sizes unchanged
}
```

## Infinite constraints: invariants and guards

Infinities enter from exactly one place: a `Max` set to `+∞` so a child reports its
**natural** size on an axis the parent won't bound — a scroll pane measuring its content
(`LooseMainUnbounded`, `HeightUnbounded`) or an intrinsic-size query
(`Constraints.Unbounded`). That is legal and necessary. The hazard is letting an infinity
flow into a place that expects a finite number. Three such places, three rules.

### Invariants

> 1. `+∞` is legal **only** as a `Max`, **only** on the axis being measured for intrinsic
>    size. `Min` is always finite; `Min ≤ Max`.
> 2. A **tight** constraint is never infinite. `Tight(size)` requires a finite `size` — this
>    is the single tripwire.
> 3. `MeasureContent` always **returns a finite size**. Under an unbounded axis a view
>    returns its *natural* extent on that axis, never the (infinite) `Max`.

### The chokepoint asserts

```csharp
public static Constraints Tight(Size s)
{
    Debug.Assert(float.IsFinite(s.Width) && float.IsFinite(s.Height),
        "Tight constraint cannot be infinite — a parent tried to force an unbounded size " +
        "(usually Stretch cross-axis or grow under an unbounded main). See W1-Layout.md.");
    return new(s.Width, s.Width, s.Height, s.Height);
}

public Size Measure(Constraints c)
{
    Debug.Assert(float.IsFinite(c.MinWidth) && float.IsFinite(c.MinHeight) &&
                 c.MinWidth <= c.MaxWidth && c.MinHeight <= c.MaxHeight, "ill-formed constraints");
    // ... cache check ...
    var size = c.Constrain(MeasureContent(c));
    Debug.Assert(float.IsFinite(size.Width) && float.IsFinite(size.Height),
        $"{GetType().Name}.MeasureContent returned a non-finite size under " +
        "an unbounded constraint — return natural size, not Max.");
    // ...
}
```

`Constrain` does not save you here: `Clamp(natural, 0, +∞)` is `natural` (fine) only because
rule 3 guarantees `MeasureContent` returned a finite `natural`. If a container instead
returned its incoming `Max`, the assert in `Measure` fires immediately at the offending view.

### Two defined fallbacks (not bugs) in `FlexView`

These amend the flex algorithm so an infinity never *reaches* a `Tight`:

- **Step 1 (cross), `Stretch` under an unbounded cross** → degrade to natural cross. A
  vertical `FlexView` inside a *horizontal* scroll pane is measured with unbounded width
  (cross). `Stretch` cannot tighten to `+∞`, so it falls back to measuring the child
  cross-loose and using its natural cross — CSS's "stretch with an indefinite cross size
  resolves to the child's content size."
  ```csharp
  var cross = (CrossAxisAlignment == Stretch && c.HasBoundedCross(Axis))
      ? boundsCross                          // finite → tight is safe
      : MeasureChildCrossLoose(child);       // natural, finite
  ```
- **Steps 3–4 (free space + distribute), unbounded main** → grow/shrink are inert; every
  child keeps its basis. A `FlexView` inside a *vertical* scroll pane is measured with
  unbounded height (main); `free = ∞ − Σbasis` is meaningless, so distribution is skipped and
  children take their natural (basis) main — CSS's "flex-grow has no effect when the
  container main size is indefinite." This is also what makes a column-of-grow-children
  inside a scroll pane scroll by natural content height instead of collapsing.
  ```csharp
  if (!c.HasBoundedMain(Axis)) return;       // basis-only; no free space to distribute
  ```

Both are *defined behavior*, not asserts — the asserts above only catch the bug of forcing
an infinity through `Tight`, which these fallbacks prevent by construction.

### Scroll panes stay finite on the cross axis

A pane only ever unbounds the **scroll axis**. `ScrollPane` measures content with
`MinWidth = MaxWidth = viewport.Width` (finite, tight cross) and `MaxHeight = +∞` (unbounded
main). So content's `Stretch` cross children get a finite tight width, and only the main axis
carries the infinity — consistent with the fallbacks above. A two-axis pane unbounds both
*maxes* but still never constructs a `Tight` from them.

## V1 → V2 mapping (every container must round-trip)

| V1 container | V2 expression |
| --- | --- |
| `ColumnView` / `RowView` | `FlexView { Axis }`, no `FlexItem` children |
| `FlexColumnView` / `FlexRowView` | `FlexView { Axis }` with `FlexItem.Grow` children |
| `PaddingView` | `MeasureContent`: measure child with `c` deflated by padding, return child+padding; `ArrangeContent`: child at inset rect |
| `CenterView` | loose constraint + center returned size (above) |
| `BorderLayoutView` | measure N/S at full width, W/E at natural width against remaining height, Center gets the leftover rect — straight port, now via `Measure`/`Arrange` |
| `ScrollPane` / `VerticalScrollPane` | unbounded-main measure + arrange-time offset + clip (above); `Scroll` → `InvalidateArrange` |
| `TextView` | `MeasureContent(c)`: wrap to `c.MaxWidth` when wrapping, return `lines * lineHeight`; generic measure cache replaces `_wrappedForWidth` |
| `FlexItem` | unchanged role; carries `Grow`, read by `FlexView` |

## Phasing within W1 (land in order, keep `main` compiling between steps)

1. **Introduce types, no behavior change.** Add `Constraints`, `Axis`, axis helpers, `Size`
   (already exists). Compiles alongside V1.
2. **Add the `Measure`/`Arrange` protocol to `View`** with a temporary shim: the old
   `LayoutSelf`/constraint-field path delegates to a default `MeasureContent`/`ArrangeContent`
   so existing containers keep working untouched. Lets us port one container at a time.
3. **Split invalidation** (`InvalidateMeasure`/`InvalidateArrange` + descendant-flag bubble);
   make `SetField` route to `InvalidateMeasure`. Delete `IsChildrenDirty`'s `Any()` walk.
4. **Flip to Y-down** in `RectF`/`Bounds` and the root pass; fix the two scroll panes.
5. **Port containers** to native `MeasureContent`/`ArrangeContent` (table above), deleting
   the shim per container. Collapse the four flex containers into `FlexView`.
6. **Delete the constraint fields** (`LeftConstraint`/`WidthConstraint`/`Min*`/`Max*`/
   `Width`/`Height` as layout inputs) once no container reads them; `Width`/`Height` become
   a `Tight` constraint applied in `Measure`.
7. **Bench** a large virtualized commit/diff list against the V1 baseline; confirm
   single-measure-per-child and O(changed) relayout.

## Open questions

- **Relayout boundary optimization** (skip child arrange when re-measure returns an
  identical size under identical constraints): worth it, but defer to after step 7 — correctness first.
- ~~**`Width`/`Height` as tight constraints / fixed-size child in a grow slot.**~~
  **Resolved** — see "Sizing rule: `Width`/`Height` are preferred, never imposed" and
  "Flex resolution". `Width`/`Height` are a preferred basis that the parent's constraint
  clamps via `Constrain`; grow grows beyond the basis (grow wins), non-grow keeps the fixed
  size, `Max` freezes-and-redistributes.
- ~~**Infinite constraints hygiene.**~~ **Resolved** — see "Infinite constraints:
  invariants and guards". `+∞` is legal only as a `Max` on the measured axis; `Tight` and
  `MeasureContent`'s result assert finiteness (the two tripwires); `Stretch` degrades to
  natural cross and `Grow` goes inert under an unbounded axis (the two defined fallbacks).
