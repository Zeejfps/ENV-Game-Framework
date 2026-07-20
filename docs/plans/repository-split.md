# Repository Split

Completed 2026-07-20. This document records what was done and why; it is no longer a plan.

`ENV-Game-Framework` was split so that downstream GUI applications can consume the ZGF
framework as a submodule without dragging in the EGF stack, games, samples and asset
tooling.

Both repos stay active. This repo is the framework; `cs_games_sandbox` is where games,
experiments and the legacy EGF stack continue to be poked at.

## Outcome

| | Repo | Result |
|---|---|---|
| Framework | `Zeejfps/ENV-Game-Framework` (this repo) | 26 projects, history untouched |
| Sandbox | `Zeejfps/cs_games_sandbox` (private) | 41 projects, 1333 commits of filtered history |

**This repo kept its identity.** It is already consumed as a submodule by downstream
applications, so rewriting it or moving it to a new URL would have forced every consumer
to re-point for no benefit. The extraction went the other way: Sandbox was carved out,
and this repo simply committed the deletions forward. Consumers pinning older SHAs are
unaffected — nothing here was rewritten.

The repo is still *named* `ENV-Game-Framework`; renaming it to `ZGF` is pending and is
transparent to consumers thanks to GitHub's redirects.

### Method

`git-filter-repo` over a throwaway clone, keeping only the Sandbox paths. Sandbox
therefore carries real history and blame for everything it owns (`EasyGameFramework`
alone retains 188 commits) rather than starting from a squashed import.

Filtering was done at extraction time rather than deferred. Deferring would have meant
grafting a filtered history under an already-published repo later and rewriting every
SHA made in the meantime — painful once a repo has consumers.

## What lives where

### This repo (ZGF)

- `ZGF.Gui`, `ZGF.Gui.Desktop`, `ZGF.Gui.Metal`, `ZGF.Gui.Testing`, `ZGF.Gui.Tests`,
  `ZGF.Gui.Benchmarks`, `ZGF.Gui.MemoryDiagnostics`, `ZGF.Gui.Prototype`,
  `ZGF.Gui.iOS.SmokeTest`, `ZGF.Gui.Generator`
- `ZGF.Desktop`, `ZGF.Rendering.Metal`
- `ZGF.AppUtils`, `ZGF.Geometry`, `ZGF.Fonts`, `ZGF.Svg` (+ `ZGF.Svg.Tests`), `ZGF.Spatial`
- `ZGF.KeyboardModule`, `ZGF.KeyboardModule.GlfwAdapter`
- `Glfw.NET`, `OpenGL.NET` (plus the `glfw-natives` workflow and `build_linux-x64.*`)
- `PngSharp` as a nested submodule

### `cs_games_sandbox`

- `EasyGameFramework.*` (all), `OpenGlWrapper` (+ Tests)
- Games and benchmarks: Bricks family, Pong, DataOriented.Pong, Tetris, SnakeGame,
  SimplePlatformer, SandboxGame, OOPEcs, CombatBeesBenchmark v1/v2/v3
- `AssetImporter`
- Samples: `ZGF.Gui.Sandbox`, `LLMit`, `NodeGraphApp`, `ModelViewer`, `OpenGLSandbox`,
  `QuadTreeRendererProgram`, `SoftwareRendererModule`, `SlangIntegrationTest`,
  `tools/CompileCanvasShaders`
- `ZGF.BMFontModule`, `ZGF.ECSModule`, `ZGF.WavefrontObjModule`, `Module.GridStorage`,
  `MsdfBmpFont`
- `OpenGLBindingsGenerator`

Sandbox consumes this repo as a submodule at `ZGF/`. Vendoring a frozen copy was
rejected: an active sandbox pinned to a stale copy of the bindings drifts and rots, and
the divergence surfaces as confusing breakage months later.

Sandbox pins its own ZGF commit and updates when it wants, so framework work is never
blocked on keeping the games compiling. When a binding needs a change while working in
Sandbox, it is edited in the nested ZGF checkout and pushed from there — the same
submodule workflow used by the downstream GUI application.

## Sandbox's dependency on ZGF

The original plan claimed Sandbox needed only `Glfw.NET`, `OpenGL.NET`,
`ZGF.KeyboardModule` and `ZGF.KeyboardModule.GlfwAdapter`. That was wrong. The real
figure is **25 `ProjectReference`s across 9 projects, resolving to 12 ZGF projects**:

| Consumer | Needs from ZGF |
|---|---|
| `EasyGameFramework` | `ZGF.KeyboardModule` |
| `EasyGameFramework.Glfw` | `Glfw.NET`, `ZGF.KeyboardModule.GlfwAdapter` |
| `EasyGameFramework.OpenGL` | `OpenGL.NET` |
| `OpenGlWrapper` (+ Tests) | `OpenGL.NET`, `Glfw.NET` |
| `LLMit` | `ZGF.Desktop`, `ZGF.Gui`, `ZGF.Gui.Desktop` |
| `NodeGraphApp` | `Glfw.NET`, `OpenGL.NET`, `PngSharp`, `ZGF.AppUtils` |
| `QuadTreeRendererProgram` | `Glfw.NET`, `OpenGL.NET`, `ZGF.AppUtils`, `ZGF.Desktop`, `ZGF.Spatial` |
| `SoftwareRendererModule` | `ZGF.Geometry` |
| `ZGF.Gui.Sandbox` | `PngSharp`, `ZGF.Desktop`, `ZGF.Fonts`, `ZGF.Gui`, `ZGF.Gui.Desktop`, `ZGF.KeyboardModule.GlfwAdapter` |

`LLMit` and `NodeGraphApp` are real GUI consumers, not merely bindings consumers. This
does not change the design — Sandbox still pins one submodule — but it means the GUI
surface has a second audience, and breaking changes to it are felt beyond the downstream
application.

The relationship remains strictly one-way: nothing in ZGF references Sandbox.

## Cleanup done during the split

- **`ZGF.Gui.Sandbox` moved to Sandbox**, dissolving the only GUI -> legacy edge without
  dependency surgery — the project simply landed on the side its dependencies live on.
- **Fixed `MSB5004`**, a pre-existing defect that broke `dotnet build` at the CLI: a
  solution *folder* named `PngSharp` collided with the `PngSharp` project. The folder is
  now `PngSharp.Submodule`. Rider tolerated the collision, which is why it went unnoticed.
- **Added `ZGF.Gui.Benchmarks` and `PngSharp.Tests`** to the solution.
  `ZGF.Gui.iOS.SmokeTest` was deliberately left out — its csproj documents that exclusion
  so a desktop build does not require the iOS workload.
- **Deleted stale directories** that held only `bin`/`obj` with no csproj and no tracked
  files: `ZGF.Core`, `ZGF.Core.Desktop`, `ZGF.Observable`, `ZGF.Gui.Compose`, `Framework`,
  `FrameworkCommon`, `GlfwOpenGLBackend`, `GitGui`, `LibPNG.NET`, `LibPNG.NET Tests`,
  `SimpleEcs`, `SimplifiedFramework`, `ZnvQuadTree`, `builds`.

### Verification

Both sides were built and tested after the split. This repo: 0 errors, 375 `ZGF.Gui.Tests`
and 208 `PngSharp.Tests` passing. Sandbox: 0 errors against the submodule.

### Git LFS gotcha

The repo uses LFS, and a local clone carries only the objects currently checked out — the
first pushes of Sandbox failed on missing objects. `git lfs fetch --all` against the
GitHub remote (84M -> 175M locally) was required before the extracted history could be
pushed. Anyone re-cloning or re-filtering needs to do this first.

## Known issues, not caused by the split

- **12 `ZGF.Svg.Tests` golden-image tests fail** on `master`, and did before the split.
- Those runs drop untracked `.actual.png` / `.diff.png` files next to the goldens, which
  are not gitignored.

## Remaining work

1. **Add `Directory.Build.props` and `Directory.Packages.props`.** TFMs span net6.0 to
   net10.0 and test package versions have drifted (xunit.runner.visualstudio 2.8.2 vs
   3.1.4, Microsoft.NET.Test.Sdk 17.12.0 vs 17.14.1). Shared props would also centralize
   the `BuildIos` conditional-TFM pattern duplicated across nine csproj files.

2. **Decide the fate of `ZGF.Gui.Generator`** — netstandard2.0 Roslyn generator,
   referenced by nothing as an analyzer or otherwise.

3. **Rename this repo to `ZGF`.**

4. `Sandbox.sln` currently includes the 15 ZGF submodule projects. This makes the
   edit-bindings-from-Sandbox workflow pleasant but blurs the repo boundary; they would
   still build transitively if removed.

## Endgame

Submodule is correct while framework and application change together daily. Once
`ZGF.Gui` stabilizes, packaging `ZGF.Gui` and `ZGF.Gui.Desktop` as NuGet removes the
submodule entirely. Adding MinVer alongside `Directory.Build.props` makes that nearly
free later; retrofitting versioning across twenty projects afterwards does not.

`PngSharp` already carries full package metadata and only needs a version stamp —
publishing it to NuGet would also eliminate the nested submodule, which otherwise
requires every clone to use `--recurse-submodules` indefinitely.
