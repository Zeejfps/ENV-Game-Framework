# Getting Started — Desktop Apps with ZGF.Gui

`ZGF.Gui.Desktop` is a retained-mode, GPU-rendered GUI toolkit for building native desktop
applications in C#. You describe your UI as a tree of immutable **widgets**, wire up state with
observables, and the framework renders it through OpenGL (Windows/Linux) or Metal (macOS) and
drives it with a keyboard/mouse input stack.

This guide gets a desktop app on screen and walks through the core building blocks. For the full
widget-authoring reference (custom controls, controllers, stateful widgets, lists), read
[`../ZGF.Gui/WIDGETS.md`](../ZGF.Gui/WIDGETS.md). For a runnable, non-trivial example, see
[`../ZGF.Gui.Prototype`](../ZGF.Gui.Prototype).

## What you get

- **Declarative widgets** — a Flutter-like tree of inert records (`Box`, `Column`, `Text`,
  `Button`, `TextInput`, `ScrollArea`, …) that build once into a retained view tree.
- **Reactive state** — `State<T>`, `ObservableList<T>`, `Derived<T>`; bind any of them into a
  widget prop and the view updates in place when the value changes. No diffing, no rebuilds.
- **A built-in control set** — buttons, text inputs, scroll areas, virtual lists, data grids,
  context menus, calendars, and more, under `Components/`.
- **Multi-window** — main window plus secondary windows and popups, each with its own context.
- **Native niceties** — clipboard, window chrome/title-bar theming, window icons, per-platform
  fonts and glyph fallback, DPI scaling, and hot reload under `dotnet watch`.

## Prerequisites

- **.NET 10 SDK** (the projects target `net10.0`).
- A desktop OS: **Windows**, **macOS**, or **Linux**. The graphics backend is selected
  automatically per platform (OpenGL on Windows/Linux, Metal on macOS).

## 1. Create the project

A desktop app is a normal .NET executable that references three projects:

| Project | Why |
|---|---|
| `ZGF.Gui` | Core widgets, views, observables, `Context` |
| `ZGF.Gui.Desktop` | `GuiApp`, desktop controls, input, rendering backends |
| `ZGF.Desktop` | Windowing (`StartupConfig`, the run loop) |

Your `.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\ZGF.Gui\ZGF.Gui.csproj" />
    <ProjectReference Include="..\ZGF.Gui.Desktop\ZGF.Gui.Desktop.csproj" />
    <ProjectReference Include="..\ZGF.Desktop\ZGF.Desktop.csproj" />
  </ItemGroup>

</Project>
```

`OutputType` is `WinExe` to launch without a console window; use `Exe` if you want the console
(handy while developing). A default UI font (Inter) is embedded, so nothing else is required to
draw text.

## 2. Hello, window

The smallest possible app — a builder, some content, and `Run()`:

```csharp
using ZGF.Desktop;
using ZGF.Gui.Desktop;
using ZGF.Gui.Widgets;

using var app = GuiApp.CreateBuilder(new StartupConfig
    {
        WindowWidth = 800,
        WindowHeight = 600,
        WindowTitle = "Hello ZGF",
    })
    .Build(new Center
    {
        Child = new Text { Value = "Hello, desktop!", FontSize = 24, Color = 0xFFE0E0E0 },
    });

app.Run();
```

`CreateBuilder` starts a fluent [`GuiAppBuilder`](GuiAppBuilder.cs); `Build` takes the root
widget, resolves the backend, wires the framework services, and mounts the tree; `Run`
enters the event loop and blocks until the window closes. `using var` disposes the app (and the
whole view tree) on exit. Colors are `0xAARRGGBB`.

## 3. The mental model

A ZGF GUI is three layers. Keep them straight and everything else follows:

```
ViewModel   State + logic. Observable (State<T>, ObservableList<T>, Derived<T>). No view types.
   │        The ONLY mutable layer.
Widget      An immutable record describing structure. Build(ctx) resolves dependencies from the
   │        Context and returns child widgets. Built once; never mutated.
   ▼
View        The retained render node — layout, drawing, input. Framework-managed. You never
            write or touch these directly in app code.
```

- **Widgets are immutable templates.** All change-over-time happens through *bindings*, not by
  rebuilding widgets.
- **Everything reaches a widget through `Context`** (the per-window DI container) — view models
  and services are resolved in `Build`, never passed via constructors.
- **App code never touches `View`.** Compose existing widgets, or author a new widget (see
  `WIDGETS.md`). `Raw { View = … }` is the deliberate escape hatch.

## 4. A real example — a counter

Three files: a view model, a screen widget, and the composition root.

**CounterViewModel.cs** — owned, mutable state:

```csharp
using ZGF.Observable;

namespace HelloZgf;

public sealed class CounterViewModel
{
    public State<int> Count { get; } = new(0);

    public void Increment() => Count.Value++;
    public void Decrement() => Count.Value--;
}
```

**CounterScreen.cs** — structure, resolved from the context:

```csharp
using ZGF.Gui;
using ZGF.Gui.Desktop.Components.Controls;
using ZGF.Gui.Widgets;

namespace HelloZgf;

public sealed record CounterScreen : Widget
{
    protected override IWidget Build(Context ctx)
    {
        var vm = ctx.Require<CounterViewModel>();

        return new Box
        {
            Background = 0xFF1E1E1E,
            Children =
            [
                new Center
                {
                    Child = new Column
                    {
                        Gap = 12,
                        CrossAxis = CrossAxisAlignment.Center,
                        Children =
                        [
                            new Text
                            {
                                Value = Prop.Bind(() => $"Count: {vm.Count.Value}"),
                                FontSize = 28,
                                Color = 0xFFE0E0E0,
                            },
                            new Row
                            {
                                Gap = 8,
                                Children =
                                [
                                    new Button { Label = "−", OnClick = vm.Decrement },
                                    new Button { Label = "+", OnClick = vm.Increment },
                                ],
                            },
                        ],
                    },
                },
            ],
        };
    }
}
```

**Program.cs** — the composition root, the one place that knows which VMs exist:

```csharp
using ZGF.Desktop;
using ZGF.Gui.Desktop;
using HelloZgf;

var builder = GuiApp.CreateBuilder(new StartupConfig
{
    WindowWidth = 800,
    WindowHeight = 600,
    WindowTitle = "Counter",
});

builder.Context.AddSingleton(_ => new CounterViewModel());

using var app = builder.Build(new CounterScreen());

app.Run();
```

`Prop.Bind(() => …)` auto-tracks every observable read inside it, so the `Text` re-renders
whenever `Count` changes — no manual subscription, no rebuild.

## 5. The widget vocabulary

The common primitives (full table and per-prop details in `WIDGETS.md`):

| Widget | Purpose |
|---|---|
| `Text` | Draws text. `Value`, `Color`, `FontSize`, `Weight`, `Wrap`, `HAlign`/`VAlign` |
| `Box` | A painted rectangle. `Background`, `BorderColor`, `BorderRadius`, `BorderSize`, `Children` |
| `Padding` | Pure spacing around `Children`. `Amount` (a `PaddingStyle`) |
| `Column` / `Row` | Flex layout. `Gap`, `MainAxis`, `CrossAxis`, `Children` |
| `Center` | Centers `Child` in the available space |
| `Grow` / `Spacer` | A child that grows along the flex axis / flexible empty space |
| `BorderLayout` | `North`/`South`/`East`/`West` intrinsic edges, `Center` fills |
| `Button` | `Label` + `OnClick` (both required), `Background`, `HoverBackground`, `FontSize` |
| `TextInput` | Two-way `Value` (`Prop<string>`), `Placeholder`, clipboard + focus wired |
| `ScrollArea` | Scrollable pane with a scrollbar (needs a bounded height to engage) |
| `Image` | `ImageId`, `Tint`, `Rotation` |
| `Each.Of` | Mirrors an `ObservableList<T>` into children — dynamic lists |
| `Raw` | Embeds a hand-built `View` — the escape hatch |

All widgets share `Width`, `Height`, `MinWidth`, `MinHeight`, `Id`, and `Visible`.

## 6. State and binding

Three observable types live in `ZGF.Observable`:

- **`State<T>`** — a single mutable value. `vm.Count.Value++` notifies everything bound to it.
- **`ObservableList<T>`** — a list whose add/remove/move drive `Each.Of` to create and destroy
  child subtrees live.
- **`Derived<T>`** — a computed value that recomputes when its inputs change.

Bindings are how a built widget changes after build. Most styling and content props are
`Prop<T>`: a constant converts implicitly, and a reactive value goes through `Prop.Bind`:

```csharp
new Text { Value = Prop.Bind(() => $"{vm.Remaining()} of {vm.Tasks.Count} left") },
new Text { Value = "Empty", Visible = Prop.Bind(() => vm.Tasks.Count == 0) },
new Box  { Background = Prop.Bind(() => task.IsDone.Value ? 0xFF232A23 : 0xFF2A2A2A) },
```

You can also project a single observable directly with `.Bind`:

```csharp
new Text { Color = task.IsDone.Bind(done => done ? 0xFF6B7280u : 0xFFE0E0E0u) },
```

Any `State`/`ObservableList` read inside the function is tracked automatically; the binding
re-fires when any of them change, and its lifetime follows the view (subscribe on mount, dispose
on unmount).

Dynamic lists use `Each.Of`, which builds the item template once per element against a scoped
child context:

```csharp
new ScrollArea { Children = [ Each.Of(vm.Tasks, new TaskRow(), gap: 4) ] }
```

## 7. Wiring services (the composition root)

`Program.cs` registers view models and app services on `builder.Context`. The
framework adds its own services (input, canvas, popups, clipboard) into that same container
during `Build`, so your root widget's `Build` sees a fully-wired context.

```csharp
// Factory — runs lazily on first resolve; seed initial state here:
builder.Context.AddSingleton(_ =>
{
    var vm = new TodoViewModel();
    vm.AddTask();
    return vm;
});

// Or constructor-injected, with dependencies pulled from the container:
builder.Context.AddSingleton<TodoViewModel>();
```

Register anything shared as a singleton. `ctx.Require<T>()` on an *unregistered* constructible
type falls through to constructor injection and returns a **new instance per resolve** that
nobody disposes — fine for a throwaway helper, a bug for shared state. When in doubt, register.

## 8. Fonts, images, and the window icon

`GuiApp` exposes intent-named methods for native resources. A default font is already loaded;
register more if you need them:

```csharp
using var app = builder.Build(new AppShell());

app.RegisterFont("Inter", "Assets/Fonts/Inter-Regular.ttf", pixelSize: 16);
app.RegisterFallbackFont("Assets/Fonts/NotoSansCJK.ttc", pixelSize: 16); // consulted for missing glyphs

var logoId = app.LoadImage("Assets/logo.png"); // pass the returned id to Image.ImageId
app.SetIcon("Assets/app-icon.rgba");           // window icon (packed RGBA)

app.Run();
```

To ship asset files next to the executable, include them in your `.csproj`:

```xml
<ItemGroup>
  <Content Include="Assets\**\*">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </Content>
</ItemGroup>
```

## 9. Running and hot reload

```bash
dotnet run --project HelloZgf          # run it
dotnet watch --project HelloZgf        # run with hot reload
```

Under `dotnet watch` (or Rider), editing a widget's `Build`/`CreateView` triggers a live rebuild
of the view tree. Application state lives in the view models in the DI `Context`, not in the
views, so the rebuild preserves your state — the counter keeps its value across an edit.

## 10. Where to go next

- **[`../ZGF.Gui/WIDGETS.md`](../ZGF.Gui/WIDGETS.md)** — the full widget authoring guide: custom
  widgets, `CreateView`, controllers and input, stateful controls (`Widget<TState>`), lists and
  scoped contexts, and multi-window rules.
- **[`../ZGF.Gui.Prototype`](../ZGF.Gui.Prototype)** — a small, complete reference app (a todo
  list with add/remove/clear, a scrollable list, and a text input).
- **`Components/`** — the built-in controls: `Controls/` (button, text input, scroll area,
  virtual list), `DataGrid/`, `ContextMenu/`, `Calendar/`, scrollbars.
- **[`../ZGF.Gui.Sandbox`](../ZGF.Gui.Sandbox)** — the advanced case: a GUI composited over a
  live OpenGL scene via `UseRenderBackend` / `UseStartup` / `UseRenderHook`.
```
