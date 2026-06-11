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
        Padding = PaddingStyle.All(16),
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
new Text { Bind = () => $"{vm.RemainingCount()} of {vm.Tasks.Count} remaining" },
new Text { Value = "Nothing to do.", BindVisible = () => vm.Tasks.Count == 0 },
new Box  { BindBackground = () => task.IsDone.Value ? 0xFF232A23 : 0xFF2A2A2A, ... },
```

There is no diffing/reconciliation: a widget builds once, then bindings mutate the
retained views in place. If you find yourself wanting to "rebuild on state change," you
almost always want a binding (or `Each` for lists) instead.

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
| `Text` | `TextView` | `Value`, `FontSize`, `Color`, `Bind`, `BindColor` |
| `Box` | `RectView` | background/border/padding, `Children`, `BindBackground` |
| `Column` / `Row` | `FlexView` | `Gap`, `MainAxis`, `CrossAxis`, `Children` |
| `BorderLayout` | `BorderLayoutView` | `North`/`South`/`East`/`West` intrinsic, `Center` fills |
| `Center` | `CenterView` | centers `Child` in the available space |
| `Grow` | `FlexItem` | `Child` grows along the parent flex axis |
| `Spacer` | `FlexItem` | flexible empty space between siblings |
| `Button` | `KbmInput`→`Box`→`Text` | `Label`, `OnClick` (both `required`); pure `Build` composition |
| `Image` | `ImageView` | `ImageId`, `Tint`, `Rotation` |
| `TextInput` | `TextInputView`+controller | two-way `Value` (`State<string>`), `Placeholder`, clipboard wired |
| `ScrollArea` | scroll pane+scrollbar | wheel/drag/keys synced; needs a bounded height to engage |
| `Each<T>` / `Each.Of` | `FlexView` + children binding | dynamic lists, scoped contexts |
| `Raw` | — | embeds a prebuilt `View`; pins the widget to one window |
| `KbmInput` | controller on the child's view | desktop input as a widget: `OnClick`/hover, raw handlers, `Controller` seam |

All of these inherit shared per-view props from `Widget`: `Width`, `Height`,
`MinWidth`, `MinHeight`, `Id`, `BindVisible`.

### Input as widgets

Views are input-agnostic so each platform can interpret the same tree with its own input
stack. On desktop that interpretation is `KbmInput` (`ZGF.Gui.Desktop/Widgets/`), which wraps
a child and registers a keyboard/mouse controller on the child's built view for its mounted
lifetime — no wrapper view is inserted. Four tiers, combinable:

- **Semantic callbacks** for the common case — `OnClick` (left press, consumes),
  `OnHoverEnter`/`OnHoverExit`. They fire once per gesture, on the bubble phase.
- **Drag callbacks** — `OnDragStart`/`OnDrag(Vector2 delta)`/`OnDragEnd` plus `DragThreshold`
  (0 = drag starts on press; >0 = press arms, drag starts past the travel). The framework's
  `DragRecognizer` owns the gesture state (`_isDragging`, previous point) and the
  steal-focus/consume/blur dance; it is recreated per mount so state can't leak across
  remounts. App code stays stateless.
- **Raw handlers** (`OnMouseButton`, `OnKey`, `OnMouseWheel`, ...) see every phase and manage
  consumption themselves.
- **`Controller`** attaches a stateful `IKeyboardMouseController` built against the child's
  view — created per mount, disposed per unmount. Use it when the interaction is a state
  machine the framework doesn't own a recognizer for (text editing).

View-land code (a `View` subclass with no build context) gets the same vocabulary as plain
controllers: `KbmHandlers` (the delegate tiers as an `IKeyboardMouseController`) and
`DragRecognizer`, both registered via `UseController`. The scrollbar thumbs compose the two —
a recognizer for the drag, `KbmHandlers` for the hover highlight.

```csharp
new KbmInput
{
    OnClick = () => vm.StepMonth(-1),
    OnHoverEnter = () => button.BackgroundColor = HoverColor,
    OnHoverExit = () => button.BackgroundColor = NormalColor,
    Child = new Raw { View = button },
}
```

`view.UseController(...)` remains the imperative escape hatch inside `CreateView` when the
controller targets a view other than the widget's root (e.g. the calendar's year input). A
future mobile stack would ship its own `TouchInput` twin with touch-native semantics
(tap, drag, long-press) over the same views.

With `KbmInput`, an interactive control can be pure `Build` composition — no views, no
`Raw`: `Button` is `KbmInput` → `Box` (with `BindBackground` over a local
`State<bool> hovered`) → `Text`.

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
    public required State<bool> Value { get; init; }

    protected override View CreateView(Context ctx)
    {
        var view = new RectView { ... };
        view.BindBackgroundColor(() => Value.Value ? OnColor : OffColor);
        view.UseController(ctx.Require<InputSystem>(), () => new ToggleController(view, Value));
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
