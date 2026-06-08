# ZGF.Gui V2 — Redesign Notes & Plan of Attack

Status: **planning**. This is a living document. Each workstream has a rationale, the
evidence that motivated it (with `file:line` pointers into the V1 tree), a proposed
direction, and a checklist. Update the checklists as work lands; keep the rationale so we
don't relitigate decisions.

The scope is the GUI stack: `ZGF.Gui` (core view/layout/reactive) and `ZGF.Gui.Desktop`
(input, windowing, backends). Evidence references are from V1 as consumed by the GitBench
app, which is the primary real-world driver for these changes.

## Guiding principles

- **Correctness by construction over convention.** If the framework requires a call by
  convention (e.g. `SetDirty()`), and forgetting it produces an invisible bug, that's a
  design defect to remove — not document.
- **One way to do reactivity.** V1 grew three overlapping notions of "a value that
  changes" (`View.SetField`/dirty, `ZGF.Observable`, `StyleValue<T>`). Converge on one.
- **Pay layout cost proportional to what changed**, not to tree size.
- **Don't break the parts that work.** See "Keep these" — these are not up for redesign.

## Keep these (validated by V1, carry forward unchanged in spirit)

- **The behavior model.** `IViewBehavior` + `Behaviors.Add(...)` attach/detach lifecycle
  (`ZGF.Gui/IViewBehavior.cs`, `View.cs:617-636`). `UseViewModel`/`Use`/`UseController`
  riding on context attach is the right shape.
- **Auto-tracked derivations.** `DependencyTracker` thread-static collector so
  `Derived<T>` / `BindText(() => ...)` discover their own dependencies
  (`Observables/DependencyTracker.cs`, `Derived.cs`).
- **Fine-grained list binding.** `ObservableList<T>` → `BindChildren` producing matching
  child mutations with no diff (`Observables/ObservableList.cs`,
  `Bindings/BindingExtensions.cs:195-225`).
- **Generation-lane background work.** `ViewModelBase` + `GenerationGuard` per-concern
  lanes dropping stale continuations across repo switches (GitBench `ViewModelBase.cs`).

---

## Workstreams (ordered by leverage-per-risk)

### W1 — Layout engine rewrite

> **Detailed design: [W1-Layout.md](./W1-Layout.md)** — concrete `Constraints`/`Measure`/
> `Arrange` API, the single `FlexView`, split invalidation, a V1→V2 mapping table for every
> container, and a 7-step phasing plan. The summary below is the rationale only.

**Why.** Three compounding defects in the V1 layout pass.

1. *Measure and arrange are conflated and children are re-measured 2–3× per pass.*
   `OnLayoutSelf` measures and positions in one call (`View.cs:412-459`);
   `FlexColumnView.OnLayoutChildren` measures each child again for total height, then
   again after grow distribution (`Views/FlexColumnView.cs:59,135`); `TextView` re-wraps a
   further time in `OnDrawSelf` (`Views/TextView.cs:157`). Hot path for virtualized
   commit/diff lists.
2. *Dirty does not propagate; reading it walks the subtree.*
   `IsChildrenDirty => _children.Any(child => child.IsDirty)` is O(subtree) on every
   access (`View.cs:180-182`); `SetDirty()` only sets the local flag and never notifies
   the parent (`View.cs:407-410`). A clean node still pays a recursive walk to discover a
   dirty descendant.
3. *Constraints are mutable public fields the parent writes into the child.*
   Parents assign `child.LeftConstraint/WidthConstraint/...` directly during layout
   (`Views/FlexColumnView.cs:146-149`), each assignment routing through `SetField` →
   `SetDirty` and re-dirtying the child mid-layout. The child exposes `Width`,
   `WidthConstraint`, `Min/MaxWidthConstraint` simultaneously with implicit precedence
   resolved in `OnLayoutSelf`; intrinsic size is not queryable without running layout.

Also: *4× duplicated container algorithms.* `FlexColumnView`/`FlexRowView`/`ColumnView`/
`RowView` are one algorithm with main/cross axes swapped — fixes must land four times.

And: *Y-up coordinate system.* `Position` uses `Left`/`Bottom` with `currentTop -= ...`
decrement math; `VerticalScrollPane` carries `1f - scrollOffset` reversal. Y-up buys
nothing for a 2D document UI and taxes every container author.

**Direction.**
- Two-phase protocol where constraints are *passed as arguments* and sizes *returned*,
  not stored as mutable child state: `Size Measure(Constraints c)` (pure, cacheable on
  `(constraint, content-version)`), then `void Arrange(RectF finalRect)`.
- `SetDirty()` marks the node and bubbles a "child-dirty" bit up to the root so the
  layout walk visits only dirty paths — O(changed), not O(tree).
- Single `FlexView { Axis }` with a main/cross axis abstraction over `Size`/`RectF`;
  delete the four duplicated containers.
- Flip to Y-down; remove the scroll-offset reversal hacks.

**Checklist.**
- [ ] Define `Constraints` (min/max width+height) and the `Measure`/`Arrange` contract.
- [ ] Measure cache keyed on `(Constraints, content-version)`; invalidation on content change.
- [ ] Dirty-bit propagation (local + ancestors-have-dirty-child); layout walk skips clean subtrees.
- [ ] Single axis-parameterized `FlexView`; port Column/Row/FlexColumn/FlexRow callers.
- [ ] Switch coordinate system to Y-down; update `ScrollPane`/`VerticalScrollPane`.
- [ ] Bench against a large virtualized list (commits/diff) vs V1 baseline.

### W2 — Layout-only invalidation (delete `SetDirty()` as a convention)

> **Detailed design: [W2-Properties.md](./W2-Properties.md)** — scope decision (continuous
> redraw stays), typed layout setters, the property reclassification table, uniform redraw,
> the custom-drawn-view path, and phasing. Summary below is rationale.

**Why.** The most repeated footgun in app code — every themed view ends with property
assignments + a hand-written `SetDirty()` (GitBench `CommitsView`, `BranchesView`,
`LocalChangesPanel` epilogues), enforced by nothing. And it's *wasteful*: `SetDirty()` forces
a layout pass (`View.cs:407-410`) even for a colour change that moves no geometry.

**Scope decision.** Continuous per-frame redraw **stays** — change-detection for the GPU is
the renderer's job and `RenderedCanvasBase.UploadIfChanged` already does it. The framework's
job is to minimize **layout CPU**, which W1 already gates. Dirty-gated *presentation* is
explicitly out of scope.

**Direction.** Because every frame redraws, an appearance-only change (colour, highlight,
theme) needs **no invalidation at all** — it lands next frame for free. So invalidation
collapses to W1's two **layout** tiers: split `SetField` into `SetMeasure`/`SetArrange` for
geometry-affecting properties; demote appearance properties to plain auto-properties; and
**delete** the appearance-change `SetDirty()` calls (they only bought needless layout). Make
all windows redraw uniformly (drop the popup `NeedsRedraw` gate, `OpenGlApp.cs:139`) so
dirty-gated popups stop missing async/animated changes. No paint tier, no redraw pump, no
animation pump. Keep `State<T>` only for two-way control state.

**Checklist.**
- [ ] Split `SetField` into `SetMeasure`/`SetArrange`; demote appearance props to plain.
- [ ] Reclassify built-in properties (geometry → setter; appearance → plain).
- [ ] Delete app-side `SetDirty()` on appearance/theme/selection reactions.
- [ ] Uniform continuous redraw: drop the `NeedsRedraw` gate for popup/secondary windows.
- [ ] Delete `SetDirty()` from the `View` surface once no caller remains.

### W3 — Unify the reactive + value primitives

**Why.** Three parallel "value that changes" systems: `View.SetField`/dirty,
`ZGF.Observable` (`State`/`Derived`/`ObservableList`), and `StyleValue<T>`'s has-value/
is-set struct that participates in neither (`StyleValue.cs`). Crossing one gap touches all
three (`state.Subscribe` → mutate `StyleValue` field → `SetDirty()`), and views accumulate
cached-state fields (`_listing`, `_selection`, `_snapshot`, `_rows`) that exist only to
stash what a `Subscribe` lambda received.

**Direction.** One reactive primitive. View properties are observables; bindings are
`dst.Bind(src)`. Replace `StyleValue<T>`'s "unset" with `T?`/optional rather than a
parallel struct. Shrinks per-view cached-state fields. Depends on W2.

**Checklist.**
- [ ] Decide the single primitive surface (`IReadable<T>`/`State<T>` family stays; fold in view props).
- [ ] Retire `StyleValue<T>` in favor of optionals; migrate constraint/size properties.
- [ ] Sweep app views for now-redundant cached-state fields.

### W4 — Pointer-capture input model

**Why.** `InputSystem` repeats the capture→bubble walk five times, identical except the
callback (`Desktop/Input/InputSystem.cs` `SendKeyboardKeyEvent`/`SendMouseButtonEvent`/
`SendMouseScrollEvent`/`SendMouseMovedEvent`/enter-exit). The comments map the pain:
button dispatch rebuilds the path from the cursor because "_focusQueue is still the stale
path… otherwise the user has to click a second time" (`InputSystem.cs:166-176`); `Reset()`
exists because pooled popups go "dead until the app restarts" (`InputSystem.cs:328-342`);
`PointerOwnershipArbiter` exists to stop recurring hover/focus-steal bugs. Root cause:
focus, hover, and the dispatch path (`_focusQueue`) are three loosely-coupled mutable
states that desync.

**Direction.** Generic `Dispatch(path, phaseFilter, handler)` collapsing the five copies.
Model **explicit pointer capture** — a capturing controller owns the pointer stream until
it releases (standard pointer-capture semantics) — instead of re-inferring the path from
hover each event and patching desyncs. Subsumes drag hacks, stale-path rebuild, and the
popup-pool reset into one invariant.

**Checklist.**
- [ ] Extract the single generic capture/bubble dispatch routine.
- [ ] Explicit pointer-capture API (acquire/release); migrate drag + popup outside-click.
- [ ] Re-evaluate whether `PointerOwnershipArbiter` is still needed; fold in or delete.
- [ ] Remove `Reset()`/stale-path workarounds once capture owns the invariant.

### W5 — Typed composition; drop reflection from `Context`

**Why.** `Context` is a runtime service locator with reflection-based construction
(`Context.cs:42-69` `Create<T>`, `Require<T>` at `:33-35`). Dependencies are invisible and
fail at runtime, not compile time (`ctx.Require<IRepoRegistry>()` buried in a view body).
Reflection over public ctors is AOT/trimming-hostile — the `DynamicallyAccessedMembers`
annotation is the tell — and blocks any future NativeAOT desktop build.

**Direction.** Keep an ambient `Context` for genuinely ambient services (Canvas, theme,
dispatcher), but make VM/controller construction explicit factories registered at the
composition root rather than reflected. Every `UseViewModel<T>` already names `T`, so this
costs nothing in practice and buys compile-time wiring + AOT-friendliness.

**Checklist.**
- [ ] Split ambient services from constructed types.
- [ ] Replace `Context.Create`/reflection with registered factories.
- [ ] Verify trimming/AOT viability on a smoke target.

### W6 — Declarative single/conditional children (`BindChild`)

**Why.** Tree assembly verbosity (70–150-line nested trees) and the held-child-reference
pattern trace mostly to *imperative child swapping*: a `_panelChild`/`_currentBody` field
plus a `ReferenceEquals`-guarded swap helper, because there's no declarative "show this
view when state == X" (GitBench `LocalChangesPanel.SetBody`, `DiffView.SetPanelChild`,
`StashDialog.RenderFiles`). `BindChildren` already solves this for lists.

**Direction.** Extend the list-binding idea to single/conditional slots:
`container.BindChild(vm.Mode, mode => mode switch { ... })`. Removes a large fraction of
held-reference fields and manual swap methods.

**Checklist.**
- [ ] `BindChild(IReadable<T>, Func<T, View>)` with identity-stable reuse.
- [ ] Migrate the imperative swap sites in the app.

### W7 — Styling ergonomics (lower priority, incremental)

**Why.** Views redeclare 6–20 near-identical `TextStyle` fields; dialogs hand-build
`DialogShell` + `Body` + validation identically ~10×. No style reuse/composition.

**Direction.** Theme-scoped style tokens (shared, composable `TextStyle` values derived
with `with`); a `FieldDialog` scaffold parameterized by fields + primary action. Pure
app-side ergonomics — land incrementally after the core workstreams.

**Checklist.**
- [ ] Style-token table on the theme; collapse per-view `TextStyle` boilerplate.
- [ ] `FieldDialog` base; port existing dialogs.

---

## Sequencing

1. **W1 Layout** — foundation; perf + correctness root.
2. **W2 Observable props** — high payoff, mechanical; removes the `SetDirty()` convention.
3. **W3 Unify reactivity** — depends on W2.
4. **W4 Pointer capture** — contained blast radius; removes a recurring bug class.
5. **W5 Typed composition** — before any AOT ambitions.
6. **W6 / W7 Ergonomics** — incremental, app-side, can interleave once the core lands.
