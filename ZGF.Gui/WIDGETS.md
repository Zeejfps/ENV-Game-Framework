# Building GUIs with Widgets

This document describes the widget pattern — the intended way to build new screens and
app-level UI. `ZGF.Gui.Prototype` is the running reference example.

## The architecture in one picture

```
Context      DI container, per window. Exists at BUILD TIME only. The ONLY way data and
   │         services reach UI code.
Widgets      Inert records. Build(ctx) resolves dependencies and returns structure.
   │         One BuildView(ctx) call at the window recurses to the leaves.
   ▼
Views        Retained tree: layout, drawing, dirty tracking, input. Carries NO context —
             every dependency was injected when it was built. Never written by hand in
             app code — only widgets construct them.
```

Views know nothing about `Context`. A built view holds exactly the dependencies its
constructor received (e.g. `TextView` holds the `ICanvas` it measures with), which means a
view is **pinned to the window it was built for** — its controllers are registered with that
window's `InputSystem`, its text measures against that window's canvas.

## View lifecycle: Mount / Unmount

A view tree is *live* while mounted. The window calls `Mount()` on its root; adding a child
to a mounted parent mounts it, removing unmounts it. Behaviors (bindings, controllers, VM
holders) attach on mount and dispose their subscriptions on unmount — `View` is not
`IDisposable`, because leaving the tree *is* the disposal signal, and it's reversible:
re-adding a subtree re-subscribes everything. App code never calls Mount/Unmount; it falls
out of tree operations (and `Each`'s add/remove does the right thing automatically).

A GUI is three kinds of objects:

| Layer | What it is | Mutable? | Example |
|---|---|---|---|
| ViewModel | State + logic. Observable (`State<T>`, `ObservableList<T>`, `Derived<T>`). No view types. | yes — the only mutable layer | `TodoViewModel` |
| Widget | A record describing structure. Resolves its VM from `Context` in `Build`. | no — init-only, inert | `TodoScreen`, `TaskRow` |
| View | Retained render node. | framework-managed | `RectView`, `FlexView` |

## The rules

1. **Everything comes from Context.** Widgets never receive ViewModels or services through
   constructors. `Build(ctx)` resolves them: `ctx.Require<TodoViewModel>()`. Per-item data
   reaches item widgets through *scoped* child contexts (see Lists below).
2. **No constructor parameters, anywhere.** Widgets use init-only properties. Mandatory
   props are marked `required` so the compiler still enforces them:
   `new Button { Label = "Add", OnClick = vm.AddTask }`.
3. **Widgets are immutable templates.** A widget instance can be built any number of
   times, against any window's context. All mutable state lives in ViewModels; all
   change-over-time on screen happens through bindings.
4. **App code never touches `View`.** If a screen needs something the built-in widgets can't
   express, either write a new widget (see below) or use `Raw { View = ... }` as an
   explicit, deliberate escape hatch.

## Writing a screen

A screen is a composite widget: subclass `Widget`, override `Build`, resolve the VM, return
structure. (`Widget` has two override seams — `Build` to compose, `CreateView` to construct
views; override exactly one.) Never call `BuildView` yourself — the recursion happens once,
at the window.

```csharp
public sealed record TodoScreen : Widget
{
    protected override IWidget Build(Context ctx)
    {
        var vm = ctx.Require<TodoViewModel>();
        return Layout(vm);
    }

    private static IWidget Layout(TodoViewModel vm) => new Box
    {
        Background = 0xFF1E1E1E,
        Children =
        [
            new Padding
            {
                Amount = PaddingStyle.All(16),
                Children =
                [
                    new Column
                    {
                        Gap = 10,
                        Children =
                        [
                            new Text { Value = "Tasks", FontSize = 20 },
                            new Button { Label = "Add", OnClick = vm.AddTask },
                            Each.Of(vm.Tasks, new TaskRow(), gap: 4),
                        ],
                    },
                ],
            },
        ],
    };
}
```

Break large layouts into private static helper methods (`Header(vm)`, `Footer(vm)`) or into
further widgets. Use a widget (not a helper) when the piece is reusable elsewhere or needs
its own context resolution; use a helper when it's just visual decomposition.

## Wiring it up (the composition root)

`Program.cs` is the only place that knows which concrete VMs exist. Register them on the
builder's `Services` context; the content factory runs after all framework services
(input, canvas, popups, clipboard) are registered, so `Build` sees a fully wired container.

```csharp
var builder = GuiApp.CreateBuilder(new StartupConfig { ... });

builder.Services.AddSingleton(_ =>
{
    var vm = new TodoViewModel();
    // seed initial state here — the factory runs lazily on first resolve
    return vm;
});

using var app = builder.UseContent(new TodoScreen()).Build();
app.Run();
```

VM lifetimes:

- **Registered singleton** — one instance per context, shared by every widget that
  resolves it, disposed by the context on shutdown (if container-created and `IDisposable`).
  Use for screen/feature VMs.
- **Unregistered constructible** — `ctx.Require<T>()` falls through to constructor injection
  and returns a *new instance per resolve* that nobody disposes. Acceptable for throwaway
  helpers; a bug for anything shared. When in doubt, register.

## Binding: how the screen changes after build

Widgets describe the initial structure; everything dynamic flows through auto-tracked
bindings declared as `Func<T>` props on widgets. Any `State<T>`/`ObservableList<T>` read
inside the function is tracked; the binding re-fires when any of them change, and its
lifetime follows the view (subscribe on mount, dispose on unmount).

```csharp
new Text { Value = Prop.Bind(() => $"{vm.RemainingCount()} of {vm.Tasks.Count} remaining") },
new Text { Value = "Nothing to do.", Visible = Prop.Bind(() => vm.Tasks.Count == 0) },
new Box  { Background = Prop.Bind(() => task.IsDone.Value ? 0xFF232A23 : 0xFF2A2A2A), ... },
```

Most styling props are `Prop<T>`: a constant converts implicitly (`Amount = PaddingStyle.All(8)`),
a reactive value goes through `Prop.Bind(() => …)` (any observable read inside is auto-tracked).
The same channel makes layout props reactive too — `Height = Prop.Bind(() => …)`,
`Amount = Prop.Bind(() => …)` — with no per-property `Bind*` companion.

There is no diffing/reconciliation: a widget builds once, then bindings mutate the
retained views in place. If you find yourself wanting to "rebuild on state change," you
almost always want a binding (or `Each` for lists) instead.

## `Prop<T>` vs `State<T>`

Two reactive types, two jobs. The line between them is **ownership**:

- **`Prop<T>` is a widget's *input*** — a value handed *in* by the parent. Every authoring prop is
  a `Prop<T>`, one- or two-way. The parent picks the source (constant, VM `State`, projection, or
  `Prop.Bind` compute) and the widget never learns which. A `Prop` can also write back
  (`CanWrite`/`Write`), so an *editable* input stays a `Prop` — a constant in a two-way slot just
  pins the value.
- **`State<T>` is *owned, mutable* state** — held by a ViewModel, or by a stateful widget's state
  object. It is the only writable layer; visuals react to it through bindings. It is never a widget
  input.

`CheckboxWidget` shows both, and the seam between them:

```csharp
public sealed record CheckboxWidget : Widget<CheckboxState>
{
    public Prop<bool> Checked { get; init; }                 // input: the caller owns the value

    protected override CheckboxState CreateState(Context ctx) =>
        new(Checked.ToReadable(ctx), Checked.Write);         // bridge the Prop into owned state
}

public sealed class CheckboxState : ICheckbox
{
    private readonly State<bool> _hovered = new(false);      // owned: live interaction state
    private readonly State<bool> _pressed = new(false);
}
```

`Checked` is a `Prop<bool>` because the *caller* owns it — `new CheckboxWidget { Checked = vm.Init }`
binds VM state, `Checked = true` pins a constant, and the widget treats both the same. Hover/press
are `State<bool>` because the *checkbox* owns them: a controller writes them, the theme reads them,
nothing outside cares. `CreateState` resolves the input's read side (`ToReadable`) and write side
(`Write`) once; past that seam everything is plain owned state. `TextInput` does the same with a
two-way `Prop<string>`.

## Lists: `Each` and scoped contexts

`Each` mirrors an `ObservableList<T>` into children, one template build per item. It is also
how per-item data flows without constructor passing: each item gets a **child Context** with
the item registered as a service, and the template builds against that scope.

```csharp
// In the parent:
Each.Of(vm.Tasks, new TaskRow(), gap: 4)

// The template — note: parameterless, shared, built once per item:
public sealed record TaskRow : Widget
{
    protected override IWidget Build(Context ctx)
    {
        var list = ctx.Require<TodoViewModel>();   // parent chain → window context
        var task = ctx.Require<TaskViewModel>();   // this row's scope
        return ...;
    }
}
```

Resolution is nearest-scope-wins, so nested `Each`es of the same item type shadow correctly.
Adds/removes/moves on the source list create and destroy subtrees live; no manual wiring.
`Each.ConfigureScope` registers extra per-item services on the scope; singletons the scope
creates are disposed when the item is removed.

Constraint to design around: scopes are type-keyed. One registration per type per scope —
"what type does this item resolve as" is part of an item VM's API design.

## Primitives

The widget infrastructure lives in the framework: `IWidget`/`Widget`/`Raw` and the layout
widgets in `ZGF.Gui/Widgets/` (namespace `ZGF.Gui.Widgets`), input-bearing controls like
`Button`, `TextInput` and `ScrollArea` in `ZGF.Gui.Desktop/Components/Controls/`. The
current vocabulary:

| Primitive | Builds | Notes |
|---|---|---|
| `Text` | `TextView` | `Value` and `Color` (each a `Prop<T>`), `FontSize`, `Weight`, `Wrap`, `HAlign`/`VAlign`, `Rotation` (radians, for spinner glyphs) |
| `Box` | `RectView` | `Background`/`BorderColor`/`BorderRadius`/`BorderSize` (each a `Prop<T>`), `Children` — paints a box |
| `Padding` | `PaddingView` | `Amount` (a `Prop<PaddingStyle>`), `Children` — pure spacing, no draw |
| `Column` / `Row` | `FlexView` | `Gap`, `MainAxis`, `CrossAxis`, `Children` |
| `BorderLayout` | `BorderLayoutView` | `North`/`South`/`East`/`West` intrinsic, `Center` fills |
| `Center` | `CenterView` | centers `Child` in the available space |
| `Grow` | `FlexItem` | `Child` grows along the parent flex axis |
| `Spacer` | `FlexItem` | flexible empty space between siblings |
| `Button` | `Box`→`Text` | `Label`, `OnClick` (both `required`); pure `Build` composition |
| `Image` | `ImageView` | `ImageId`, `Tint`, `Rotation` |
| `TextInput` | `TextInputView`+controller | two-way `Value` (`Prop<string>`), `Placeholder`, clipboard wired |
| `ScrollArea` | scroll pane+scrollbar | wheel/drag/keys synced; needs a bounded height to engage |
| `Each<T>` / `Each.Of` | `FlexView` + children binding | dynamic lists, scoped contexts |
| `Raw` | — | embeds a prebuilt `View`; pins the widget to one window |
| `ScrollBar` | track `Box` + thumb | consumer supplies the thumb view as the sync handle |

All of these inherit shared per-view props from `Widget`: `Width`, `Height`,
`MinWidth`, `MinHeight`, `Id`, `Visible`.

### Wiring input: controllers on views

Views are input-agnostic so each platform can interpret the same tree with its own input
stack. Input reaches a view by attaching a **controller** to it — not by wrapping it in an
input node. Two seams cover the cases:

- **Inside `CreateView`**, `view.UseController(ctx.Require<InputSystem>(), () => new …Controller(…))`
  attaches a stateful `IKeyboardMouseController` for the view's mounted lifetime — created per
  mount, disposed per unmount. This is where a control that owns its whole interaction (text
  editing, a toggle) wires its own input, and the escape hatch when the controller targets a
  view other than the widget's root (e.g. the calendar's year input).
- **On a `Widget<TState>`** whose state implements `IInteractable`, the *parent* attaches the
  controller with `.WithController<KbmController>()` (see "Stateful controls" below), so the
  control stays neutral about input modality — a touch stack would attach its own over the
  same surface.

Both seams draw from one controller vocabulary, available to view-land code (a `View` subclass
with no build context) too. `KbmHandlers` packages semantic callbacks (`OnClick`,
`OnHoverEnter`/`OnHoverExit`, fired once per gesture on the bubble phase) and raw handlers
(`OnMouseButton`, `OnKey`, `OnMouseWheel`, …, which see every phase and manage their own
consumption) into a single `IKeyboardMouseController`. `DragRecognizer` owns drag gesture state
— `_isDragging`, the previous point, the steal-focus/consume/blur dance — and is recreated per
mount so state can't leak across remounts. The Sandbox's `VerticalListView` composes the two
for its scrollbar — a recognizer for the thumb drag, `KbmHandlers` for the hover highlight and
track click. Both register via `UseController`.

**`widget.BuildView(ctx)` is for crossing the widget→view boundary mid-tree only** (adding a
built widget into a view's `Children`, the mirror image of `Raw`). If a `CreateView` override
*ends* with `return someWidget.BuildView(ctx);`, that's `Build`'s job — override `Build` and
return the widget instead. `Build` also receives the context, so it may still construct and
wire views (via `Raw`) when it needs to; the difference is only who calls `BuildView`.

### Writing a new view-constructing widget

Drop down a level when a piece needs view construction, controllers, or services — i.e. when
`Build` returning other widgets isn't enough. Override `CreateView` instead of `Build`; this
is the only place app-adjacent code constructs Views and wires behaviors:

```csharp
public sealed record Toggle : Widget
{
    public Prop<bool> Value { get; init; }   // an input, so a Prop — see "Prop<T> vs State<T>"

    protected override View CreateView(Context ctx)
    {
        var value = Value.ToReadable(ctx);
        var view = new RectView { ... };
        view.BindBackgroundColor(() => value.Value ? OnColor : OffColor);
        view.UseController(ctx.Require<InputSystem>(),
            () => new ToggleController(view, value, Value.Write));
        return view;
    }
}
```

`CreateView` is where build-time injection happens: text/image views take `ctx.Canvas`,
controllers take `ctx.Require<InputSystem>()`, and anything a controller needs at runtime
(clipboard, coordinates, app services) is resolved here and passed through its constructor.
Nothing reaches back into a context after build.

Generic widgets should also ship a static factory for type inference (`Each.Of`), since
C# does not infer generic arguments from constructors or initializers.

### Stateful controls: a state object the parent wires

`Toggle` above stays imperative — `CreateView` builds the view *and* attaches the controller in
one place. That's fine when the control owns its whole interaction. But a richer control (a
checkbox, an action button) has live interaction state — hover, press, enabled — that three
parties touch: a controller *writes* it, the theme *reads* it to pick colors, and the control
*reads its rising edge* as activation. Bake that into `CreateView` and the control also ends up
deciding which input modality drives it and whether it shows a tooltip — decisions that belong to
the *parent*, not the control.

The pattern that unbundles this is `Widget<TState>` + a state object implementing `IInteractable`:

- **`CreateState(ctx)`** builds the interaction state once, before the view tree. Two-way `Prop`
  inputs are bridged into owned state here — `Checked.ToReadable(ctx)` for the read side,
  `Checked.Write` for the write side — so past this seam the control deals only in plain
  observables (see "`Prop<T>` vs `State<T>`").
- **`Build(ctx, state)`** composes visuals and binds them to that state:
  `Background = Theme.Color(s => s.Checkbox.BoxFill(state))`. The state *is* the theme's input —
  `CheckboxStyles` resolves every color from the one `ICheckbox` reference instead of loose booleans.
- The state is exposed as **`IWidget<TState>.State`**, valid only after build. `IWidget<out TState>`
  is covariant, so a `Widget<CheckboxState>` is also an `IWidget<IInteractable>` — that up-cast is
  what lets a single non-generic combinator reach the state without the caller naming `CheckboxState`.

`IInteractable` is the entire contract between the control and its controller: `Hovered`/`Pressed`
(the controller writes), `Enabled` (the controller reads to know whether to). The control wires
*activation* in its state's constructor — the rising edge of `Pressed` runs the command / flips the
value — so nothing downstream re-implements "what does a press mean":

```csharp
public sealed class CheckboxState : ICheckbox            // ICheckbox : IInteractable
{
    private readonly State<bool> _hovered = new(false);  // owned: the controller writes these
    private readonly State<bool> _pressed = new(false);  // (Hovered/Pressed/Enabled exposed — elided)
    public IReadable<bool> Checked { get; }              // the bridged read side of the Prop

    public CheckboxState(IReadable<bool> @checked, Action<bool> writeChecked)
    {
        Checked = @checked;
        _pressed.Changed += pressed =>                   // rising edge of press = activation
        {
            if (pressed) writeChecked(!@checked.Value);
        };
    }
}
```

#### The parent attaches behavior: `WithController`, `WithTooltip`

The control stays neutral about *modality* and *decoration*; the parent composes those on with
combinators that read the exposed `State`:

```csharp
new CheckboxWidget { Label = "Remove even if dirty", Checked = vm.Force }
    .WithController<KbmController>()
```

- **`WithController<TController>()`** (framework, `ZGF.Gui.Desktop/Controllers`) wires a DI-built
  controller onto the built view, injecting the widget's `State` as the `IInteractable` it drives.
  The parent picks `KbmController` — and thus keyboard/mouse as the modality — while the control only
  supplies the state; a touch stack would later attach its own controller over the same surface.
  (Overloads cover view-aware controllers and external peer targets — see `WidgetControllerExtensions`.)
- **`WithTooltip("…")`** (app layer, GitBench's `TooltipWidgetExtensions`) hangs a hover tooltip off
  the *same* state, reading its `Hovered`/`Enabled`. It no-ops on an unset string, so a tooltip is
  opt-in per call site instead of a field every control has to carry.

Chaining order is **tooltip, then controller**, and the types enforce it: `WithTooltip` returns
`IWidget<TState>` (state preserved, so it can come first), `WithController` returns a plain
`IWidget` (state consumed). `ActionButton` is the same `Widget<TState>` shape and reads identically:

```csharp
new ActionButton { Command = vm.OpenFolder, Children = [new ButtonIcon { Value = LucideIcons.FolderOpen }] }
    .WithTooltip("Open in file explorer")
    .WithController<KbmController>()
```

Contrast `Button`, which needs none of this: its only interaction state is a local
`State<bool> hovered` and a single `OnClick` wired by a controller on its own view.
Reach for `Widget<TState>` + `IInteractable` when the control has a real hover/press/enabled state
machine that a controller drives, the theme reads, and that carries activation — not for a plain click.

#### Converting an interactive View to this shape

1. Move the live booleans (hover/press/checked/enabled) out of the View into a small state class
   implementing `IInteractable` (or a superset like `ICheckbox`); wire activation as a
   `Pressed.Changed` rising-edge handler in its constructor.
2. Make the widget a `Widget<TState>`; build the state in `CreateState`, bridging any two-way input
   `Prop` through `ToReadable`/`Write`.
3. In `Build(ctx, state)`, return plain widgets and bind visuals via `Theme.Color(s => …(state))` —
   no `Raw`, no hand-built views, where you can avoid it.
4. Delete the View's own controller wiring. Let the parent attach `.WithController<KbmController>()`
   (and optionally `.WithTooltip(…)`) at the call site.

### When to subclass `View` instead

Widgets compose; they do not lay out or paint. Write a `View` subclass only for:

- a new layout/paint algorithm (something that overrides `Measure*`/`OnLayout*`/`OnDrawSelf`),
- a widget with an imperative peer protocol — a sibling view's layout or controller drives it
  through a typed handle (scrollbars ↔ scroll pane, `ContextMenuItem` ↔ `ContextMenu`'s
  shortcut-column layout, `CalendarDayCell` ↔ the calendar grid's refresh).

`View.Children` is protected by default: a view decides whether it accepts arbitrary
children. General containers (`ContainerView`, `RectView`, `FlexView`, `PaddingView`,
`CenterView`) re-expose the collection with `public new ChildrenCollection Children`;
slot-style views (`FlexItem`, `BorderLayoutView`) keep it protected and accept content only
through their slots. Children bindings (`BindChildren`) live on the collection, so they're
only available where children are public.

Then expose it to widget land with a wrapper. The reference example is
`ZGF.Gui.Desktop/Components/Calendar/Calendar.cs`: a `Widget` whose `CreateView` resolves
`CalendarViewModel` from the context, assembles plain views (the day cells stay Views), wires
controllers, and binds VM state to a refresh routine.

## ViewModels

- Observable state via `State<T>` / `ObservableList<T>` / `Derived<T>`; plain methods as
  commands. No view types, no `Context` — VMs receive *their* dependencies via constructor
  (the container injects them when registered with `AddSingleton<T>()`).
- View-derived facts a host needs to query (focus, scroll position) are exposed as a
  VM property whose implementation the building widget supplies:
  `vm.FocusProbe = () => ReferenceEquals(input.FocusedComponent, controller);`
- Constructor args = the VM's own dependencies. Widget init-props = static config.
  Context = how VMs reach widgets. Keep those three channels straight.

## Multi-window

Widgets are window-agnostic; built Views are not. The rule:

- Share **widgets** freely — build the same instance in any window's context.
- Never move a **built View** between windows. To show the "same" UI elsewhere, build the
  widget again against that window's context.

The framework enforces this: every window owner takes a build factory, not a view —
`GuiAppBuilder.UseContent(Func<Context, View>)`, `SecondaryWindowRequest.BuildRoot`,
`PopupRequest.BuildRoot`, and `IContextMenuHost.ShowContextMenu(Func<Context, ContextMenu>, …)`.
Each window invokes the factory with its **own** context (its canvas, its `InputSystem`, its
`IWindowCoordinates`), so popup menus and secondary windows are built fresh, correctly wired,
on every show.

## Known gaps

- Grow the widget vocabulary on demand; `Raw` in app code is the signal a wrapper is missing.
- Legacy composite `View` subclasses coexist fine — embed via `Raw { View = ... }` until
  each is converted to a widget + VM.
