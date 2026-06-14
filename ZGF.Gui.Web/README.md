# ZGF.Gui.Web

Browser (WebAssembly) host for ZGF GUI, using the lightweight **.NET WASM
browser-app** model (`System.Runtime.InteropServices.JavaScript`), not Blazor —
the eventual renderer drives a `<canvas>` + WebGL2 directly.

> **Status: skeleton (first draft, never compiled/run here).** Two things run:
> the **font validation spike** (`FontSpike.Run`) — proves FreeType (our self-built
> `libfreetype.a`) + HarfBuzz (the published wasm asset) work under browser-wasm —
> and a **WebGL2 render demo** (`Rendering/WebGl2RenderedCanvas`) driven from
> `Program.Tick()`, drawing a rect + text + shapes directly against `ICanvas`.
> There is no view/layout/input shell yet; `Resize` is wired but DOM input is not.

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
| `webgl2.js` | WebGL2 shim: handle tables + flat function surface |
| `main.js` | runtime bootstrap, runs the spike, sizes the canvas, RAF render loop |
| `index.html` | page + `#zgf-canvas` render target |

The WebGL2 backend lives here for now; it could be extracted to a `ZGF.Gui.WebGL2`
package later (mirroring `ZGF.Gui.Metal`) once a second web host needs it.

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
