# Web font rendering: building FreeType for `browser-wasm`

Status: **design / accepted plan** (not yet implemented)
Scope: how ZGF GUI produces shaped, rasterized text in the browser when the
canvas is rendered via WebGL2 / .NET WASM.
Decision owner: rendering.

---

## 1. Decision

When ZGF GUI runs in the browser (.NET WebAssembly + a WebGL2 canvas backend),
we keep the **existing font pipeline unchanged** and make it run in-browser by
supplying the two native libraries it depends on as `browser-wasm` static
libraries:

- **Shaping — HarfBuzz:** use the published `HarfBuzzSharp.NativeAssets.WebAssembly`
  NuGet package (matches our `HarfBuzzSharp 8.3.0.1`). No custom build.
- **Rasterization + metrics — FreeType:** **we build `libfreetype.a` ourselves**
  with Emscripten and link it via `<NativeFileReference>`. FreeTypeSharp ships
  native assets for Windows/Linux/macOS/Android/iOS but **not** `browser-wasm`,
  so this is the one piece we own.

The result: `ZGF.Fonts.FreeTypeFontBackend` compiles and runs **verbatim** in the
browser, producing pixel-identical text (same hinting, anti-aliasing, synthetic
bold, OpenType features) to desktop. Everything downstream — the glyph atlas, the
dirty-rect upload protocol, and the GPU draw path — is already platform-neutral
and needs no font-specific changes.

We chose this over the two alternatives:

- **Managed rasterizer (StbTrueTypeSharp):** no native build, but rasterization
  diverges from desktop FreeType (different hinting/AA) and loses
  `FT_GlyphSlot_Embolden`. Kept as a *fallback* if the Emscripten build ever
  becomes too costly to maintain.
- **Browser Canvas2D `fillText`:** best system-text quality but fights our
  architecture — Canvas2D rasterizes whole strings with no glyph indices, while
  our GPU path is per-glyph instancing keyed by glyph index into an atlas.
  Adopting it would force a rewrite of the text draw path and diverge from
  desktop. Rejected.

---

## 2. Background: what the font layer owes the canvas

`RenderedCanvasBase` (in `ZGF.Gui`) is GPU-API-agnostic. For text it depends only
on `ZGF.Fonts.FreeTypeFontBackend`, which provides four things:

| Job | Implementation | Native? |
|-----|----------------|---------|
| **Atlas packing** — skyline allocator, R8 byte buffer, dirty-rect tracking | `ZGF.Fonts/GlyphAtlas.cs` | No (pure C#) |
| **Shaping** — text → glyph indices + offsets/advances/clusters, OpenType features | HarfBuzz | Yes |
| **Rasterization** — glyph outline → 8-bit grayscale bitmap | FreeType | Yes |
| **Metrics / variants** — ascender, descender, line height, sized + emboldened variants | FreeType | Yes |

Key facts that make this tractable:

1. **The GPU side never sees FreeType.** The atlas is just an `R8` byte buffer
   uploaded via `glTexSubImage2D` (desktop) / `replaceRegion` (Metal). The web
   backend will upload the identical buffer via `texSubImage2D` (WebGL2). So this
   is *not* a rendering problem — it is purely "produce the same `ShapedGlyph[]`,
   `GlyphRenderInfo`, and atlas bytes in the browser."
2. **Shaping does not depend on FreeType.** `FreeTypeFontBackend` loads the
   HarfBuzz face straight from the font bytes (`new Face(hbBlob, 0)`), independent
   of the FreeType face. So the HarfBuzz and FreeType halves can be sourced
   separately.
3. **`GlyphAtlas` is already platform-neutral** and is reused as-is on every
   platform, including web.

---

## 3. Architecture

```
                 ZGF.Gui (RenderedCanvasBase)  ── depends on ──►  IGlyphSource
                                                                      ▲
                          ┌───────────────────────────────────────────┤
                          │                                           │
                 FreeTypeFontBackend  (ZGF.Fonts)             (future managed
                 ├─ HarfBuzz  (shaping)                        StbTrueType backend,
                 ├─ FreeType  (rasterize + metrics)            fallback only)
                 └─ GlyphAtlas (shared, pure C#)

      Native libs resolved per-RID:
        desktop  : FreeTypeSharp + HarfBuzzSharp default native assets
        ios      : FreeTypeSharp + HarfBuzzSharp ios assets
        browser  : HarfBuzzSharp.NativeAssets.WebAssembly (NuGet)
                 + our self-built libfreetype.a  (NativeFileReference)
```

### 3.1 Prerequisite refactor: extract `IGlyphSource`

`RenderedCanvasBase` currently takes a concrete `FreeTypeFontBackend`. Extract an
interface in `ZGF.Fonts` covering exactly the members the canvas calls, so a
different rasterizer can be substituted later without touching the canvas:

```csharp
public interface IGlyphSource
{
    int   ShapeText(FontHandle font, ReadOnlySpan<char> text, Span<ShapedGlyph> output, in FontFeatureSet features);
    bool  TryGetGlyph(FontHandle font, uint glyphIndex, out GlyphRenderInfo info);
    FontMetrics GetMetrics(FontHandle font);
    FontHandle  GetSizedVariant(FontHandle baseFont, int pixelSize);
    FontHandle  GetEmboldenedVariant(FontHandle baseFont);

    // Atlas surface consumed by the backend upload hooks.
    int  AtlasWidth { get; }
    int  AtlasHeight { get; }
    ReadOnlySpan<byte> AtlasPixels { get; }
    bool AtlasDirty { get; }
    AtlasDirtyRect DirtyRect { get; }
    void ClearDirty();
}
```

`FreeTypeFontBackend` already implements this surface; the change is mechanical
(add `: IGlyphSource`, retype the `RenderedCanvasBase._fonts` field and ctor
param). **No behavior change.** This is safe to land independently and ahead of
any web work.

> For the FreeType-on-wasm plan specifically, the web backend literally reuses
> `FreeTypeFontBackend` — the interface is for the *fallback* option and for
> keeping the canvas honest about its dependency. Do it anyway; it's cheap.

---

## 4. Building FreeType for `browser-wasm`

### 4.1 Toolchain: match .NET's pinned Emscripten exactly

A `browser-wasm` static library **must** be built with the same Emscripten
version that the .NET WASM SDK links with, or the objects will fail to link
(ABI / `wasm-ld` / bitcode mismatches). Do **not** use a system `emsdk` at an
arbitrary version.

Find the pinned version from the installed runtime pack rather than guessing:

```sh
# Install the wasm build tooling first.
dotnet workload install wasm-tools

# The pinned Emscripten version is recorded in the Emscripten runtime pack that
# the wasm-tools workload pulls in. Locate it under the dotnet packs folder:
find ~/.dotnet ~/.nuget -iname 'Microsoft.NET.Runtime.Emscripten*' -maxdepth 6 2>/dev/null

# The version is encoded in the pack name, e.g.
#   Microsoft.NET.Runtime.Emscripten.<EMSDK_VERSION>.Node.* 
# Use that exact EMSDK_VERSION with emsdk below.
```

Then provision a matching emsdk:

```sh
git clone https://github.com/emscripten-core/emsdk.git
cd emsdk
./emsdk install  <EMSDK_VERSION>     # the version discovered above
./emsdk activate <EMSDK_VERSION>
source ./emsdk_env.sh
emcc --version                        # sanity-check it matches
```

> Pin `<EMSDK_VERSION>` in the build script and in CI. When we bump the .NET SDK,
> re-run the discovery step and update the pin in the same change.

### 4.2 Configure a minimal FreeType

We only need outline rasterization + metrics + embolden. Disable every optional
dependency so the build is self-contained (no zlib/libpng/brotli/harfbuzz). This
keeps the `.a` small and avoids dragging in more native libs.

Use a FreeType release tarball that matches the FreeTypeSharp 3.0.1 ABI (FreeType
2.13.x). Build with CMake driven by `emcmake`:

```sh
# from the FreeType source tree
emcmake cmake -B build-wasm -S . \
  -DCMAKE_BUILD_TYPE=Release \
  -DBUILD_SHARED_LIBS=OFF \
  -DFT_DISABLE_ZLIB=ON \
  -DFT_DISABLE_BZIP2=ON \
  -DFT_DISABLE_PNG=ON \
  -DFT_DISABLE_BROTLI=ON \
  -DFT_DISABLE_HARFBUZZ=ON \
  -DCMAKE_C_FLAGS="-O2 -fPIC"

emmake make -C build-wasm -j

# Output: build-wasm/libfreetype.a  (a browser-wasm static archive)
```

Notes:
- `-fPIC` and `-O2` are required for clean linking into the .NET wasm app and to
  keep glyph rasterization fast.
- Keep autohinter + TrueType + CFF drivers enabled (the defaults). They are what
  produce our current desktop AA; disabling them would change output.
- The resulting archive exports the standard `FT_*` C symbols that FreeTypeSharp
  P/Invokes — no shim or wrapper is needed.

### 4.3 Wire it into the web host project

In the **browser host** project (the new `ZGF.Gui.Web` host, see §5), reference
the archive as a native dependency and enable AOT (native deps on wasm require an
AOT publish):

```xml
<PropertyGroup>
  <RuntimeIdentifier>browser-wasm</RuntimeIdentifier>
  <WasmBuildNative>true</WasmBuildNative>
  <RunAOTCompilation>true</RunAOTCompilation>
</PropertyGroup>

<ItemGroup>
  <!-- Self-built; produced by tools/build-freetype-wasm.sh, checked into a
       binaries location or restored from CI artifacts (see §7). -->
  <NativeFileReference Include="native/wasm/libfreetype.a" />

  <!-- Shaping: published native asset, no custom build. -->
  <PackageReference Include="HarfBuzzSharp" Version="8.3.0.1" />
  <PackageReference Include="HarfBuzzSharp.NativeAssets.WebAssembly" Version="8.3.1.3" />
</ItemGroup>
```

`HarfBuzzSharp.NativeAssets.WebAssembly` self-registers its `NativeFileReference`
via `$(HarfBuzzSharpStaticLibraryPath)` (the package's build props); we only have
to add our FreeType archive manually.

---

## 5. C# integration

### 5.1 Reuse `FreeTypeFontBackend` verbatim

Because the `.a` exports the same `FT_*` symbols, `ZGF.Fonts` compiles unchanged
against `browser-wasm`. The only project change is exposing a `browser-wasm`
target. Mirror the existing opt-in `BuildIos` pattern with a `BuildWasm` switch
so a normal desktop build never drags in wasm tooling:

```xml
<!-- ZGF.Fonts.csproj, ZGF.Gui.csproj, etc. -->
<TargetFrameworks>net10.0</TargetFrameworks>
<TargetFrameworks Condition="'$(BuildIos)'=='true'">net10.0;net10.0-ios</TargetFrameworks>
<TargetFrameworks Condition="'$(BuildWasm)'=='true'">$(TargetFrameworks);net10.0-browser</TargetFrameworks>
```

### 5.2 New projects (parallel to the desktop/iOS hosts)

- `ZGF.Gui.WebGL2` — the `RenderedCanvasBase` subclass that uploads instance
  buffers and issues draws through WebGL2 (JS interop). Font-independent; covered
  by the separate WebGL2 backend plan.
- `ZGF.Gui.Web` — the browser host: a `<canvas>`, a `requestAnimationFrame`
  loop calling `BeginFrame`/draw/`EndFrame`, DOM-event → input mapping, and DI
  wiring that constructs `FreeTypeFontBackend` as the `IGlyphSource`. This is the
  project that carries the `NativeFileReference` and AOT settings from §4.3.
  **A skeleton of this exists** (`.NET` WASM browser-app model, `[JSExport]`):
  it carries the native wiring and runs the §7 font spike (`FontSpike.Run`);
  `Program.Tick()` is the render-loop seam awaiting the WebGL2 backend.

No font selection logic is needed: on `browser-wasm` the same
`FreeTypeFontBackend` is constructed as on desktop. The `IGlyphSource` seam exists
so the managed fallback can be swapped in later without touching the canvas.

---

## 6. Build & CI integration

- **`tools/build-freetype-wasm.sh`** — encapsulates §4.1–§4.2: provisions the
  pinned emsdk, downloads + verifies the FreeType source tarball (checksum),
  configures, builds, and emits `native/wasm/libfreetype.a`. Idempotent; no-op if
  the archive is present and the pin is unchanged.
- **Artifact handling** — building FreeType on every `dotnet build` is wasteful.
  Build the `.a` in a dedicated CI job (or once locally) and either (a) commit it
  under `native/wasm/` with the emsdk version encoded alongside, or (b) publish it
  as a CI artifact restored before the wasm publish. Prefer (b) if we want to
  avoid binaries in git; prefer (a) for reproducible local web builds without CI.
- **Keep wasm off the critical path** — gate everything behind `-p:BuildWasm=true`
  exactly like the iOS smoke test gates on `BuildIos`. A normal solution build on
  any OS must not require the wasm-tools workload, emsdk, or the FreeType archive.
- **Add a wasm smoke test** mirroring `ZGF.Gui.iOS.SmokeTest`: a `net10.0-browser`
  class library referencing `ZGF.Gui` + `ZGF.Fonts` that forces the toolkit to
  compile under the browser target and surfaces restore-time native-asset gaps
  early. Exclude it from `ENV Game Framework.sln`.

---

## 7. Validation spike (½–1 day, do this first)

Run before committing to the full host. Each step gates the next.

1. **Project setup** — `net10.0-browser` Blazor/wasm console app; `wasm-tools`
   workload installed; `RunAOTCompilation=true`; it publishes and loads in a
   browser.
2. **Shaping in-browser** — add `HarfBuzzSharp` +
   `HarfBuzzSharp.NativeAssets.WebAssembly`; shape `"Affinity"` and log glyph
   indices + advances. Proves the published shaping asset works under AOT.
3. **Build FreeType** — run `tools/build-freetype-wasm.sh`; `NativeFileReference`
   the archive; `FT_Init_FreeType` + load an embedded `.ttf` +
   `FT_Load_Glyph(..., FT_LOAD_RENDER)` for `'A'`; dump the grayscale bytes and
   verify non-empty, plausible dimensions. **This validates the entire
   `NativeFileReference` + AOT + emsdk-match pipeline — the riskiest part.**
4. **End-to-end atlas** — construct `FreeTypeFontBackend`, call `TryGetGlyph`,
   confirm `AtlasPixels`/`DirtyRect` populate as on desktop.

If 2–4 pass, the web font story is effectively done; the remaining work is the
WebGL2 backend and the host shell, both font-independent.

---

## 8. Risks & open questions

| Risk | Likelihood | Mitigation |
|------|-----------|------------|
| emsdk version drift vs .NET SDK breaks linking | Medium | Pin `<EMSDK_VERSION>`; discover from the Emscripten runtime pack; re-pin on SDK bumps |
| FreeType source version vs FreeTypeSharp 3.0.1 ABI mismatch | Low–Med | Build FreeType 2.13.x to match FreeTypeSharp 3.0.1; pin the tarball + checksum |
| AOT publish time / output size regresses dev loop | Medium | wasm build gated behind `BuildWasm`; AOT only on publish, not inner-loop debug |
| `HarfBuzzSharp.NativeAssets.WebAssembly` version vs our `8.3.0.1` managed | Low | Keep managed + native HarfBuzz majors aligned (8.3.x); bump together |
| Atlas R8 (`GL_RED`) upload differences on WebGL2 | Low | WebGL2 supports `R8`/`RED`; covered by the WebGL2 backend plan, not here |
| Threading: FreeType/HarfBuzz buffers are single-threaded | Low | wasm UI runs single-threaded; matches current backend's assumptions |

Open questions to resolve during the spike:
- Exact `<EMSDK_VERSION>` for the .NET 10 SDK we ship on (record it in the script).
- Commit the `.a` vs CI-artifact it (§6) — decide based on repo policy on binaries.
- Whether to also expose `net10.0-browser` from `ZGF.Geometry`/`ZGF.AppUtils`/
  `PngSharp` now or lazily when the host needs them (mirror the iOS smoke-test
  finding list).

---

## 9. Task list

- [x] Extract `IGlyphSource` in `ZGF.Fonts`; implement on `FreeTypeFontBackend`;
      retype `RenderedCanvasBase` to depend on the interface. (No behavior change.)
- [x] Add `tools/build-freetype-wasm.sh` (pinned emsdk + minimal FreeType → `.a`).
- [x] Add `BuildWasm` opt-in TFM to `ZGF.Fonts` and `ZGF.Gui` (deps are consumed
      via their existing TFMs, which `net10.0-browser` supports).
- [x] Add `ZGF.Gui.Web.SmokeTest` (`net10.0-browser` compile probe), excluded
      from the solution.
- [x] Wire `NativeFileReference` + HarfBuzz wasm package + AOT in the web host
      (`ZGF.Gui.Web` skeleton + `FontSpike` harness).
- [ ] Run the §7 spike: install `wasm-tools`, run `tools/build-freetype-wasm.sh`,
      `dotnet run --project ZGF.Gui.Web`, and record the emsdk + FreeType versions
      that link cleanly. **(needs the .NET wasm toolchain — not yet run.)**
- [~] WebGL2 canvas backend: first draft landed in `ZGF.Gui.Web/Rendering`
      (`WebGl2RenderedCanvas` + `[JSImport]` binding + `webgl2.js` shim + GLSL→ES
      adapter), wired into `Program.Tick()` with a demo draw. Unbuilt/unrun.
- [~] Web DOM input bridge (`Input/WebInput`) + clipboard (`Input/WebClipboard`)
      landed in `ZGF.Gui.Web`; the demo reacts to the pointer. Self-contained —
      does not yet drive the real controller framework. Unbuilt/unrun.
- [ ] (Architectural milestone) Extract the interaction layer (InputSystem,
      controllers, components) out of `ZGF.Gui.Desktop` into a platform-neutral
      package both desktop and web reference, so the web host can drive the real
      view/controller framework. Plus image draws on the WebGL2 backend.

> **Implementation status (this branch):** the structural items above are landed
> as a non-behavioral refactor + opt-in scaffolding (the `IGlyphSource` seam, the
> FreeType build script, the `BuildWasm` TFM, the compile smoke test, and the
> `ZGF.Gui.Web` host skeleton with its native wiring + `FontSpike`). **None of it
> has been compiled or run here** — this environment has no .NET SDK. The
> `IGlyphSource` swap is verified by inspection (every member `RenderedCanvasBase`
> calls already exists on `FreeTypeFontBackend` with matching signatures, and
> subclasses upcast their concrete backend at the `base(...)` call). Everything
> wasm-facing (the `BuildWasm` TFM, the smoke test, `ZGF.Gui.Web`) is
> gated/excluded so a normal desktop build is unaffected, and is best treated as
> the **starting point for the §7 spike** rather than known-good: expect to adjust
> package versions, the emsdk/FreeType pins, and the wasmbrowser bootstrap to what
> the installed .NET 10 SDK actually expects.
```
