# W3 — One reactive system, one optional type

Parent: [V2-Redesign.md](./V2-Redesign.md) workstream W3. Sequenced **after** W1 and W2,
which already absorbed most of the original W3 scope. Read those first; this doc is
deliberately the smallest of the three.

## What W1 and W2 already settled

The original W3 framing was "three parallel value-that-changes systems —
`View.SetField`/dirty, `ZGF.Observable`, and `StyleValue<T>` — unify them." After W1/W2 the
landscape is mostly resolved:

- **`View.SetField`/dirty is no longer a reactive system.** W2 redefined it as plain layout
  invalidation (`SetMeasure`/`SetArrange`, or nothing for appearance). It does not compete
  with observables; it's just how a view schedules layout.
- **`StyleValue<T>` is already gone from layout.** W1 replaced the constraint/size use
  (`LeftConstraint`, `Width`, `Min*`/`Max*`) with `Constraints` + a preferred `float?`.
- **`ZGF.Observable` is the keeper.** W1's "keep these" validated auto-tracked `Derived<T>`
  and `ObservableList<T>`. It is the single reactive system going forward.
- **View properties are sinks, not observables.** W2 decided this (no per-property wrapper —
  it would tax the virtualized lists W1 speeds up). That decision stands; W3 does not revisit it.

So W3 is three concrete, bounded items, not a rewrite.

## Item 1 — Retire `StyleValue<T>` in favor of `T?`

`StyleValue<T>` (`StyleValue.cs`) is a `record struct { T? Value; bool IsSet; }` with implicit
conversions and (for the float case) arithmetic operators. After W1 removed its layout/
arithmetic use, what remains is purely the **optional / "inherit vs explicitly set"** role on
style properties — and that is exactly what `Nullable<T>` / `T?` already is.

Remaining V1 uses (post-W1):

| V1 | V2 |
| --- | --- |
| `StyleValue<uint> TextColor` (`TextView.cs:10`) | `uint? TextColor` |
| `StyleValue<float> FontSize`, `StyleValue<FontWeight>`, `StyleValue<TextAlignment>`, `StyleValue<TextWrap>`, `StyleValue<FontFeatureSet>` (`TextStyle`) | `float?`, `FontWeight?`, … |
| `StyleValue<string> FontFamily` | `string?` (reference type — already optional) |
| `StyleValue<int> Gap` (`ScrollPane.cs:32`) | `int?` |
| `StyleValue<float> Grow` (`FlexItem.cs:5`) | `float?` (read as `Grow ?? 0`) |

Mechanical mapping: `.IsSet` → `.HasValue`; `.Value` → `.Value`; `StyleValue.Unset` →
`null`; `T → StyleValue<T>` implicit → `T → T?` built-in; `(T?)styleValue` → the nullable
itself. Style resolution that currently branches on `IsSet` to fall back to a theme/default
(the canvas reading `TextStyle`) branches on `HasValue` instead — identical semantics.

Why bother: it deletes a bespoke type in favor of a BCL one every C# reader already
understands, removes the implicit-conversion surface that made `StyleValue<float>` quietly
interconvert with `float`/`float?`, and leaves exactly one "maybe-a-value" representation in
the framework.

Watch-outs:
- `StyleValue` arithmetic (`RightConstraint => LeftConstraint + WidthConstraint`,
  `View.cs:109-110`) is already deleted by W1; no operator replacement needed.
- Equality: `T?` uses `EqualityComparer<T?>` — fine for the `SetMeasure`/`Eq` change-guards.
- A `StyleValue<string>` that distinguished "set to null" from "unset" would not survive the
  collapse, but no such use exists; `string?`'s single null is sufficient.

## Item 2 — `ZGF.Observable` is the single reactive surface

Affirm and freeze the primitive set; ensure there's exactly one way to express each role and
no leftover competitor:

| Role | Type |
| --- | --- |
| Mutable source of truth | `State<T>` |
| Computed (auto-tracked) | `Derived<T>` |
| Observable collection | `ObservableList<T>` |
| Read-only handle | `IReadable<T>` |
| Optional / inherited value | `T?` (Item 1) |
| View property (sink) | plain field, bound via `Bind*` or read live (Item 3) |

This set is already non-redundant; W3's job is to **enforce** it: no new ad-hoc
change-notification (no view-local `event Action` for state, no second optional type), and
the binding surface (`Bind`, `BindText`, `BindChildren`, …, `Bindings/BindingExtensions.cs`)
stays the one way a property-backed view consumes an observable. `IReadable<T>` is the
currency everything flows through.

## Item 3 — Read-live: views hold observables, not cached values

The survey's recurring footgun — views accumulating `_listing`, `_selection`, `_snapshot`,
`_rows` fields that exist only to stash what a `Subscribe` lambda received, each paired with a
`SetDirty()`:

```csharp
// V1: cache the value + subscribe + invalidate
private Selection _selection;
vm.Selection.Subscribe(SetSelection);
private void SetSelection(Selection s) { _selection = s; SetDirty(); }   // needless layout
// OnDrawSelf reads _selection
```

W2's continuous redraw makes this unnecessary for **appearance** state: hold the *observable*
and read `.Value` at the point of use. The next frame draws the current value — no cached
field, no subscription, no `SetDirty()`:

```csharp
// V2: hold the handle, read live
private IReadable<Selection> _selection;        // assigned in Bind: _selection = vm.Selection;
// OnDrawSelf: var sel = _selection.Value;       // current value, drawn this frame
```

For **layout-driving** observables (a list whose change rebuilds rows), keep a subscription —
but it triggers the rebuild and reads live; it caches nothing and needs no `SetDirty()`
(mutating `Children` invalidates layout inherently, W1):

```csharp
vm.Listing.Subscribe(_ => RebuildRows());
private void RebuildRows() { var listing = _vm.Listing.Value; /* rebuild Children */ }
```

Net effect: the per-view cached-state fields collapse to the `_vm` reference (or a few
`IReadable<T>` handles); subscriptions remain only where a change must *do* something
(rebuild), not merely stash a value.

Why it's safe:
- **No tracking side effect.** `State.Value`/`Derived.Value` call `DependencyTracker.Register`,
  which is a no-op outside a `Derived` recompute (`DependencyTracker.cs:24`). Reading at draw
  time just returns the current value.
- **No tearing.** State is UI-thread-only; the frame loop drains the dispatcher and processes
  input (`GuiApp.HandleTick`) *before* layout+draw that same iteration, so a frame always
  draws a consistent post-update snapshot.

When to still cache: if reading `.Value` is non-trivial (a `Derived` doing real work) and the
view reads it many times per frame, cache it in a local within the draw call — not in a field.

## Decided / not revisited

- **View properties as observables** — rejected in W2 (allocation on the hot path). W3 keeps
  views as sinks. The "one reactive primitive" goal is met by *consuming* `IReadable<T>`
  uniformly, not by making every property an observable.
- **Don't over-`Derived`.** A value a view only reads at draw doesn't need to be a `Derived`;
  read the source live. Reserve `Derived` for genuine cross-source computation in the VM.

## Phasing

1. **`StyleValue<T>` → `T?`** across the style surface (`TextStyle`/`TextView`, `ScrollPane.Gap`,
   `FlexItem.Grow`); update style resolution to branch on `HasValue`. Delete `StyleValue.cs`.
2. **Read-live sweep:** for each view, drop appearance cached-fields + their
   `Subscribe(_ => SetDirty())`; hold `IReadable<T>` and read `.Value` at draw. Convert
   layout-driving subscriptions to cache-nothing rebuilds.
3. **Lint the invariant:** no new optional type, no view-local state events; new view↔VM
   wiring goes through `Bind*` or read-live.

## Open questions

- **`TextStyle` as a struct of `T?` fields vs a small class.** Item 1 turns `TextStyle` into a
  struct of nullables; confirm it's still cheap to copy at draw (it's passed by `in` to the
  canvas today). Likely fine; verify no per-draw boxing of the nullables.
- **Two-way control state** (`CheckboxView.IsChecked` as `State<bool>`) stays — confirm it's
  the *only* place a view owns a `State<T>`, so "views are sinks" has exactly one principled
  exception (interactive widgets that are their own source of truth).
