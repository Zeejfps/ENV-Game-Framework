# ZGF

A retained-mode GUI framework for .NET desktop applications, plus the rendering, text,
and windowing layers it sits on.

The repository is still named `ENV-Game-Framework` for historical reasons — it once held a
game framework (`EasyGameFramework`) alongside a pile of games and samples. Those
[moved to `cs_games_sandbox`](docs/plans/repository-split.md); what remains is the ZGF
framework, consumed by downstream applications as a git submodule.

```
git clone --recurse-submodules https://github.com/Zeejfps/ENV-Game-Framework.git
```

`PngSharp` is a nested submodule, so `--recurse-submodules` is not optional.

## Hello, window

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

`dotnet run --project ZGF.Gui.Prototype` runs a real app built this way.

## The model

Three layers, deliberately distinct:

- **ViewModel** — the only mutable layer. `State<T>`, `ObservableList<T>`, `Derived<T>`,
  `ICommand`.
- **Widget** — immutable records describing structure. Built once via
  `Build(Context)`, never mutated and never diffed.
- **View** — the retained render node the framework manages and draws.

Updates are propagated by *binding*, not by rebuilding: `Prop.Bind(() => vm.Count)`
tracks the observables it reads and marks only the affected view dirty. Layout is flex
(`Row`, `Column`, `Grow`, `Shrink`, `Gap`) plus a border layout. `Context` is a per-window
DI container that widgets resolve services from in `Build` — never via constructors.

Rendering targets Metal on macOS and OpenGL elsewhere, chosen automatically; drawing goes
through a single `ICanvas` contract so neither backend leaks into application code.

## Projects

**GUI**

| | |
|---|---|
| `ZGF.Gui` | Core toolkit — views, widgets, observables, bindings, layout, text layout, `ICanvas` |
| `ZGF.Gui.Desktop` | Desktop host — `GuiApp`, input system, control library, multi-window, hot reload |
| `ZGF.Gui.Metal` | Metal canvas implementation |
| `ZGF.Gui.Testing` | Headless harness — drives a view tree with no window or GPU |
| `ZGF.Gui.Generator` | Roslyn source generator for localization resources (consumed downstream only) |

**Platform**

| | |
|---|---|
| `ZGF.Desktop` | Windowing and run loop — `IWindow`, `StartupConfig`, GLFW/Metal backends, IME |
| `ZGF.Rendering.Metal` | Metal and Objective-C interop |
| `Glfw.NET` | GLFW bindings and patched natives (IME support, `glfwSetTextInputFocus`) |
| `OpenGL.NET` | OpenGL 4.6 bindings |

**Support**

| | |
|---|---|
| `ZGF.Fonts` | FreeType/HarfBuzz text stack — shaping, glyph atlas, bidi |
| `ZGF.Svg` | SVG parser and CPU rasterizer |
| `ZGF.Geometry` | `PointF`, `RectF`, and friends |
| `ZGF.Spatial` | Quadtree, R-tree, pooling |
| `ZGF.AppUtils` | Embedded assets and path helpers |
| `ZGF.KeyboardModule` | Backend-agnostic key enum, with a GLFW adapter |
| `PngSharp` | PNG encode/decode (nested submodule) |

Also `ZGF.Gui.Prototype` (runnable demo), `ZGF.Gui.Benchmarks`,
`ZGF.Gui.MemoryDiagnostics`, and `ZGF.Gui.iOS.SmokeTest`.

## Building

Requires the .NET 10 SDK (pinned in `global.json`).

```
dotnet build
dotnet test
```

Targets Windows, Linux, and macOS. iOS is a compile-compatibility target only, opt-in via
`-p:BuildIos=true`; `ZGF.Gui.iOS.SmokeTest` is a probe rather than an app host and is
excluded from the solution so a desktop build does not need the iOS workload.

Versions come from MinVer, and package versions are centrally managed in
`Directory.Packages.props`. Both props files scope themselves out of the `PngSharp/`
subtree — see [the split notes](docs/plans/repository-split.md#the-pngsharp-guard) before
touching them.

## Further reading

- [`ZGF.Gui.Desktop/README.md`](ZGF.Gui.Desktop/README.md) — the application-authoring guide
- [`ZGF.Gui.Testing/README.md`](ZGF.Gui.Testing/README.md) — testing a view tree headlessly
- [`docs/plans/repository-split.md`](docs/plans/repository-split.md) — what lives where, and why
