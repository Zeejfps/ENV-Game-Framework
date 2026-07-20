# Repository Split Plan

Splitting `ENV-Game-Framework` so that downstream GUI applications can consume the ZGF
framework as a submodule without dragging in the EGF stack, games, samples and asset
tooling.

Both resulting repos stay active. `ZGF` is the framework; `Sandbox` is where games,
experiments and the legacy EGF stack continue to be poked at.

## Current state

`ZGF.Gui`'s dependency closure is already clean:

```
ZGF.Gui -> PngSharp, ZGF.AppUtils, ZGF.Fonts, ZGF.Geometry, ZGF.KeyboardModule, ZGF.Svg
```

None of those reach outside the ZGF/PngSharp world.

There is exactly one GUI -> legacy edge in the repository:

- `ZGF.Gui.Sandbox` -> `EasyGameFramework`
- `ZGF.Gui.Sandbox` -> `QuadTreeRendererProgram` (an `OutputType=Exe` referenced as a library)

`ZGF.Gui.Sandbox` (the project — not the repo of the same name proposed below) is a
sample host, not part of the toolkit.

No game or EGF project references `ZGF.Gui.*` at all. The relationship is one-way.

### Co-owned projects

| Project | GUI side consumers | EGF/games side consumers |
|---|---|---|
| `Glfw.NET` | ZGF.Desktop, ZGF.KeyboardModule.GlfwAdapter, NodeGraphApp, QuadTreeRendererProgram | EasyGameFramework.Glfw, OpenGlWrapper.Tests |
| `OpenGL.NET` | ZGF.Desktop, ZGF.Gui.Desktop, NodeGraphApp, QuadTreeRendererProgram | EasyGameFramework.OpenGL, OpenGlWrapper |
| `ZGF.KeyboardModule` | ZGF.Gui, ZGF.Gui.Desktop, ZGF.Gui.Testing, ZGF.Desktop | EasyGameFramework (transitively every game) |
| `ZGF.KeyboardModule.GlfwAdapter` | ZGF.Desktop, ZGF.Gui.Sandbox | EasyGameFramework.Glfw |
| `ZGF.BMFontModule` | ZGF.Gui.Sandbox only | EasyGameFramework.GUI.Backgend |

## Decision: two repositories, not three

A separate bindings repository shared by both sides is rejected. Both repos stay
active, so the argument is not about which side is alive — it is about which side
drives churn. `Glfw.NET` and `OpenGL.NET` change because of ZGF.Gui work; Sandbox
consumes them and does not push changes back. A third repo would turn every binding
tweak into a two-repo commit-and-bump cycle during exactly the workflow this split is
meant to smooth out.

The shared code lives with the side that changes it. The consuming side pins a
submodule.

### Repo A — `ZGF` (consumed as a submodule by downstream apps)

- `ZGF.Gui`, `ZGF.Gui.Desktop`, `ZGF.Gui.Metal`, `ZGF.Gui.Testing`, `ZGF.Gui.Tests`,
  `ZGF.Gui.Benchmarks`, `ZGF.Gui.MemoryDiagnostics`, `ZGF.Gui.Prototype`,
  `ZGF.Gui.iOS.SmokeTest`
- `ZGF.Desktop`, `ZGF.Rendering.Metal`
- `ZGF.AppUtils`, `ZGF.Geometry`, `ZGF.Fonts`, `ZGF.Svg` (+ `ZGF.Svg.Tests`),
  `ZGF.Spatial`
- `ZGF.KeyboardModule`, `ZGF.KeyboardModule.GlfwAdapter`
- `Glfw.NET`, `OpenGL.NET`
- `PngSharp` stays a nested submodule

### Repo B — `Sandbox`

- `EasyGameFramework.*` (all), `OpenGlWrapper` (+ Tests)
- All games and benchmarks: Bricks family, Pong, DataOriented.Pong, Tetris, SnakeGame,
  SimplePlatformer, SandboxGame, OOPEcs, CombatBeesBenchmark v1/v2/v3
- `AssetImporter`
- Samples: `ZGF.Gui.Sandbox`, `LLMit`, `NodeGraphApp`, `ModelViewer`, `OpenGLSandbox`,
  `QuadTreeRendererProgram`, `SoftwareRendererModule`, `SlangIntegrationTest`,
  `tools/CompileCanvasShaders`
- `ZGF.BMFontModule`, `ZGF.ECSModule`, `ZGF.WavefrontObjModule`, `Module.GridStorage`,
  `MsdfBmpFont`
- `OpenGLBindingsGenerator`

Repo B takes ZGF as a submodule for `Glfw.NET`, `OpenGL.NET`, `ZGF.KeyboardModule` and
`ZGF.KeyboardModule.GlfwAdapter`. Vendoring a frozen copy is explicitly rejected: an
active sandbox pinned to a stale copy of the bindings will drift and rot, and the
divergence surfaces as confusing breakage months later.

Sandbox pins its own ZGF commit and updates when it wants, so ZGF work is never blocked
on keeping the games compiling. When a binding does need a change while working in
Sandbox, it is edited in the nested ZGF checkout and pushed from there — the same
submodule workflow used by the downstream GUI application.

### Consequence: ZGF gains a second consumer

ZGF is now consumed by both the GUI application and Sandbox. Two independent consumers
strengthen the case for the NuGet endgame below, and mean breaking changes to
`ZGF.KeyboardModule` or the bindings now have a real (if forgiving) second audience.

## Work to do during the split

There are no pre-split blockers. Nothing about the current repo prevents the extraction;
everything below is cleanup done as part of the move.

1. **`ZGF.Gui.Sandbox` moves to the Sandbox repo**, alongside `EasyGameFramework` and
   `QuadTreeRendererProgram`. This dissolves the only GUI -> legacy edge without any
   dependency surgery — the project simply lands on the side its dependencies already
   live on. Consider renaming it (`ZGF.Gui.Playground`) to avoid confusion with the
   repo name.

2. **Add `Directory.Build.props` and `Directory.Packages.props` to the ZGF repo.**
   Current TFMs span net6.0 to net10.0 and test package versions have already drifted
   (xunit.runner.visualstudio 2.8.2 vs 3.1.4, Microsoft.NET.Test.Sdk 17.12.0 vs 17.14.1).
   Shared props also centralize the `BuildIos` conditional-TFM pattern currently
   duplicated across nine csproj files.

3. **Decide the fate of `ZGF.Gui.Generator`** — netstandard2.0 Roslyn generator,
   referenced by nothing as an analyzer or otherwise.

4. **Clean up stale directories** containing only `bin`/`obj` with no csproj:
   `ZGF.Core`, `ZGF.Core.Desktop`, `ZGF.Observable`, `ZGF.Gui.Compose`, `Framework`,
   `FrameworkCommon`, `GlfwOpenGLBackend`, `GitGui`, `LibPNG.NET`, `LibPNG.NET Tests`,
   `SimpleEcs`, `SimplifiedFramework`, `ZnvQuadTree`.

5. **Add the four projects missing from the solution** to whichever repo they land in:
   `ZGF.Gui.Benchmarks`, `ZGF.Gui.iOS.SmokeTest`, `tools/CompileCanvasShaders`,
   `PngSharp.Tests`.

## Endgame

Submodule is correct while framework and application change together daily. Once
ZGF.Gui stabilizes, packaging `ZGF.Gui` and `ZGF.Gui.Desktop` as NuGet removes the
submodule entirely.

Adding MinVer plus `Directory.Build.props` during the split makes that nearly free
later. Retrofitting versioning across twenty projects afterwards does not.

`PngSharp` already carries full package metadata and only needs a version stamp —
publishing it to NuGet would also eliminate the nested submodule, which otherwise
requires every clone to use `--recurse-submodules` indefinitely.
