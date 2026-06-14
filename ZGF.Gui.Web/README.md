# ZGF.Gui.Web

Browser (WebAssembly) host for ZGF GUI, using the lightweight **.NET WASM
browser-app** model (`System.Runtime.InteropServices.JavaScript`), not Blazor —
the eventual renderer drives a `<canvas>` + WebGL2 directly.

> **Status: skeleton (first draft, never compiled/run here).** What runs: the
> **font validation spike** (`FontSpike.Run`) — proves FreeType (our self-built
> `libfreetype.a`) + HarfBuzz (the published wasm asset) work under browser-wasm —
> a **WebGL2 render demo** (`Rendering/WebGl2RenderedCanvas`) driven from
> `Program.Tick()`, and a **DOM input bridge** (`Input/WebInput`) + browser
> **clipboard** (`Input/WebClipboard`). The demo reacts to the pointer
> (hover/press + a live cursor marker). There is still no view/layout/controller
> shell — see "Input & the interaction layer" below.

## Prerequisites

1. The .NET wasm build tooling:
   ```sh
   dotnet workload install wasm-tools
   ```
2. The self-built FreeType archive (this project `NativeFileReference`s it):
   ```sh
   tools/build-freetype-wasm.sh        # produces native/wasm/libfreetype.a
   ```
   This needs an Emscripten SDK matching .NET's pinned version — the script
   auto-detects and provisions it. See `docs/web-font-rendering.md` §4.

## Run

```sh
dotnet run -c Release --project ZGF.Gui.Web
```

Open the served URL; the page runs the spike and prints either:

- `PASS — FreeType + HarfBuzz are working under browser-wasm.` with atlas/metrics
  details, or
- `FAIL — …` with the exception, which is the to-do list for the native wiring.

## Layout

| File | Role |
|------|------|
| `Program.cs` | `Main` + `[JSExport]` `RunFontSpike` / `StartAsync` / `Tick` / `Resize` + the demo draw |
| `FontSpike.cs` | the §7 spike: load font, shape, rasterize, read atlas |
| `Rendering/WebGl2RenderedCanvas.cs` | `RenderedCanvasBase` backend on WebGL2 (port of the desktop GL backend) |
| `Rendering/Webgl2.cs` | `[JSImport]` binding over the WebGL2 shim (int-handle model) |
| `Rendering/GlslEs.cs` | rewrites desktop `#version 410` GLSL → GLSL ES 3.00 |
| `Input/WebInput.cs` | DOM input bridge: `[JSExport]` pointer/key/wheel callbacks + polled snapshot |
| `Input/WebClipboard.cs` | `IClipboard` over async `navigator.clipboard` |
| `webgl2.js` / `clipboard.js` | WebGL2 shim (handle tables) / clipboard shim |
| `main.js` | bootstrap: spike, canvas sizing, DOM listeners, RAF render loop |
| `index.html` | page + `#zgf-canvas` render target |

The WebGL2 backend lives here for now; it could be extracted to a `ZGF.Gui.WebGL2`
package later (mirroring `ZGF.Gui.Metal`) once a second web host needs it.

## Input & the interaction layer

`Input/WebInput` is a **self-contained** bridge: it does not reuse the desktop
interaction layer (the `InputSystem`, view controllers, and components like
`TextInput`/scroll bars all live in `ZGF.Gui.Desktop`, which is coupled to
GLFW/OpenGL/Metal and cannot be referenced from a browser build). So today the
host can *render* the full toolkit and *observe* input, but it cannot yet drive
the real view/controller framework.

Making the web host fully interactive needs that interaction layer extracted into
a **platform-neutral package** (e.g. `ZGF.Gui.Interaction`) that both the desktop
and web hosts reference — a sizable, architecturally significant refactor of
`ZGF.Gui.Desktop`. That is the recommended next milestone; it's intentionally
**not** attempted here because it would touch the working desktop build broadly
and should be scoped deliberately.

## Not in the solution

Excluded from `ENV Game Framework.sln` so a normal desktop build needs neither the
wasm-tools workload nor the native archive. Build/run it explicitly as above, or
from a dedicated CI job.

## Caveats

This skeleton has **not** been built or run in the environment it was authored in
(no .NET SDK there). Treat the first `dotnet run` as the actual validation step —
expect to adjust package versions (HarfBuzz wasm asset vs. managed wrapper), the
emsdk/FreeType pins, and the wasmbrowser bootstrap to whatever the installed
.NET 10 SDK expects. See `docs/web-font-rendering.md` §7–§8.
