# ZGF.Gui.Web

Browser (WebAssembly) host for ZGF GUI, using the lightweight **.NET WASM
browser-app** model (`System.Runtime.InteropServices.JavaScript`), not Blazor —
the eventual renderer drives a `<canvas>` + WebGL2 directly.

> **Status: skeleton.** The only runnable behavior today is the **font validation
> spike** (`FontSpike.Run`), which proves FreeType (our self-built `libfreetype.a`)
> and HarfBuzz (the published wasm asset) work under browser-wasm. The WebGL2
> canvas backend and the `BeginFrame`/`EndFrame` render loop are a separate plan;
> `Program.Tick()` is the seam where they plug in.

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
| `Program.cs` | `Main` + `[JSExport]` `RunFontSpike` / `Tick` |
| `FontSpike.cs` | the §7 spike: load font, shape, rasterize, read atlas |
| `main.js` | runtime bootstrap, runs the spike, RAF seam |
| `index.html` | page + `#zgf-canvas` (future render target) |

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
