# Building GUIs with Components

This document describes the component pattern prototyped in `ZGF.Gui.Prototype`. It is the
intended way to build new screens and app-level UI. The retained `View` system underneath is
unchanged — components are a layer on top of it.

## The architecture in one picture

```
Context      DI container, per window. The ONLY way data and services reach UI code.
   │
Components   Inert records. Build(ctx) resolves dependencies and returns structure.
   │         One BuildView(ctx) call at the window recurses to the leaves.
   ▼
Views        Retained tree: layout, drawing, dirty tracking, input. Never written by hand
             in app code — only primitives construct them.
```

A GUI is three kinds of objects:

| Layer | What it is | Mutable? | Example |
|---|---|---|---|
| ViewModel | State + logic. Observable (`State<T>`, `ObservableList<T>`, `Derived<T>`). No view types. | yes — the only mutable layer | `TodoViewModel` |
| Component | A record describing structure. Resolves its VM from `Context` in `Build`. | no — init-only, inert | `TodoScreen`, `TaskRow` |
| View | Retained render node. | framework-managed | `RectView`, `FlexView` |

## The rules

1. **Everything comes from Context.** Components never receive ViewModels or services through
   constructors. `Build(ctx)` resolves them: `ctx.Require<TodoViewModel>()`. Per-item data
   reaches item components through *scoped* child contexts (see Lists below).
2. **No constructor parameters, anywhere.** Components use init-only properties. Mandatory
   props are marked `required` so the compiler still enforces them:
   `new Button { Label = "Add", OnClick = vm.AddTask }`.
3. **Components are immutable templates.** A component instance can be built any number of
   times, against any window's context. All mutable state lives in ViewModels; all
   change-over-time on screen happens through bindings.
4. **App code never touches `View`.** If a screen needs something the primitives can't
   express, either write a new primitive (see below) or use `Raw { View = ... }` as an
   explicit, deliberate escape hatch.

## Writing a screen

A screen is a composite component: subclass `Component`, override `Build`, resolve the VM,
return structure. Never call `BuildView` yourself — the recursion happens once, at the window.

```csharp
public sealed record TodoScreen : Component
{
    protected override IComponent Build(Context ctx)
    {
        var vm = ctx.Require<TodoViewModel>();
        return Layout(vm);
    }

    private static IComponent Layout(TodoViewModel vm) => new Box
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
further components. Use a component (not a helper) when the piece is reusable elsewhere or
needs its own context resolution; use a helper when it's just visual decomposition.

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

- **Registered singleton** — one instance per context, shared by every component that
  resolves it, disposed by the context on shutdown (if container-created and `IDisposable`).
  Use for screen/feature VMs.
- **Unregistered constructible** — `ctx.Require<T>()` falls through to constructor injection
  and returns a *new instance per resolve* that nobody disposes. Acceptable for throwaway
  helpers; a bug for anything shared. When in doubt, register.

## Binding: how the screen changes after build

Components describe the initial structure; everything dynamic flows through auto-tracked
bindings declared as `Func<T>` props on primitives. Any `State<T>`/`ObservableList<T>` read
inside the function is tracked; the binding re-fires when any of them change, and its
lifetime follows the view (subscribe on attach, dispose on detach).

```csharp
new Text { Bind = () => $"{vm.RemainingCount()} of {vm.Tasks.Count} remaining" },
new Text { Value = "Nothing to do.", BindVisible = () => vm.Tasks.Count == 0 },
new Box  { BindBackground = () => task.IsDone.Value ? 0xFF232A23 : 0xFF2A2A2A, ... },
```

There is no diffing/reconciliation: a component builds once, then bindings mutate the
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
public sealed record TaskRow : Component
{
    protected override IComponent Build(Context ctx)
    {
        var list = ctx.Require<TodoViewModel>();   // parent chain → window context
        var task = ctx.Require<TaskViewModel>();   // this row's scope
        return ...;
    }
}
```

Resolution is nearest-scope-wins, so nested `Each`es of the same item type shadow correctly.
Adds/removes/moves on the source list create and destroy subtrees live; no manual wiring.

Constraint to design around: scopes are type-keyed. One registration per type per scope —
"what type does this item resolve as" is part of an item VM's API design.

## Primitives

The current vocabulary (`ZGF.Gui.Prototype/Components/`):

| Primitive | Builds | Notes |
|---|---|---|
| `Text` | `TextView` | `Value`, `FontSize`, `Color`, `Bind`, `BindColor` |
| `Box` | `RectView` | background/border/padding, `Children`, `BindBackground` |
| `Column` / `Row` | `FlexView` | `Gap`, `MainAxis`, `CrossAxis`, `Children` |
| `Grow` | `FlexItem` | `Child` grows along the parent flex axis |
| `Spacer` | `FlexItem` | flexible empty space between siblings |
| `Button` | `RectView`+`TextView`+controller | `Label`, `OnClick` (both `required`) |
| `Each<T>` / `Each.Of` | `FlexView` + children binding | dynamic lists, scoped contexts |
| `Raw` | — | embeds a prebuilt `View`; pins the component to one window |

All primitives inherit shared per-view props from `Primitive`: `Width`, `Height`,
`MinWidth`, `MinHeight`, `Id`, `BindVisible`.

### Writing a new primitive

Drop down a level when a piece needs view construction, controllers, or services — i.e. when
`Build` returning other components isn't enough. Subclass `Primitive` (to inherit shared
props) and implement `CreateView`; this is the only place app-adjacent code constructs Views
and wires behaviors:

```csharp
public sealed record Toggle : Primitive
{
    public required State<bool> Value { get; init; }

    protected override View CreateView(Context ctx)
    {
        var view = new RectView { ... };
        view.BindBackgroundColor(() => Value.Value ? OnColor : OffColor);
        view.UseController(_ => new ToggleController(view, Value));
        return view;
    }
}
```

Generic primitives should also ship a static factory for type inference (`Each.Of`), since
C# does not infer generic arguments from constructors or initializers.

### When to subclass `View` instead

Components compose; they do not lay out or paint. Write a `View` subclass only for:

- a new layout/paint algorithm (something that overrides `Measure*`/`OnLayout*`/`OnDrawSelf`),
- a widget with an imperative peer protocol (scrollbars ↔ scroll pane).

Then expose it to component land with a primitive wrapper.

## ViewModels

- Observable state via `State<T>` / `ObservableList<T>` / `Derived<T>`; plain methods as
  commands. No view types, no `Context` — VMs receive *their* dependencies via constructor
  (the container injects them when registered with `AddSingleton<T>()`).
- View-derived facts a host needs to query (focus, scroll position) are exposed as a
  VM property whose implementation the building component supplies:
  `vm.FocusProbe = () => ReferenceEquals(input.FocusedComponent, controller);`
- Constructor args = the VM's own dependencies. Component init-props = static config.
  Context = how VMs reach components. Keep those three channels straight.

## Multi-window

Components are window-agnostic; built Views are not. The rule:

- Share **components** freely — build the same instance in any window's context.
- Never move a **built View** between windows. To show the "same" UI elsewhere, build the
  component again against that window's context.

## Known gaps (prototype status)

- `Each` does not yet dispose per-item scopes on removal. Harmless today (item scopes only
  hold caller-owned registrations), required before item scopes may register factory-created
  `IDisposable`s. `BindChildren`'s `onRemoved` hook is the seam.
- `FlexItem.Child` requires a `MultiChildView`, so `Grow`/`Spacer` cast. Relax to `View` in
  `ZGF.Gui` eventually.
- The primitive vocabulary is minimal; grow it on demand (Image, ScrollPane, TextInput,
  Center wrappers are obvious next candidates).
- Legacy composite `View` subclasses (Calendar, etc.) coexist fine — embed via
  `Raw { View = ... }` until each is converted to a component + VM.
