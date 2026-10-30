# Own CEF Binding — regenerate for Chromium 150

> Moved from the Sandbox repo's `Cef150Binding/` into this repo as `CefGlue/`; `ZgfCef.Interop` is now
> `CefGlue/Xilium.CefGlue` (assembly name unchanged). The Sandbox-only harness and façade projects were
> not brought over. The renderer `__fastfail` described below turned out to be a CET shadow-stack fault
> and is fixed by `CetCompat=false` on the helper (see `docs/plans/web-view.md`).

**Status:** Phases 0–6 complete (2026-07-21). Phase 0 gate **GO**
([PHASE0-RESULT.md](PHASE0-RESULT.md)); CEF 150 payload pinned + fetched
([CEF_VERSION](CEF_VERSION), [fetch-cef.ps1](fetch-cef.ps1)); version-aware
generator forked, producing ABI-correct 150 interop ([generator/](generator/));
managed binding [ZgfCef.Interop/](ZgfCef.Interop/) (fork of Xilium.CefGlue, net10.0)
compiles + runs; **Phase 4 OSR harness [ZgfCef.Harness/](ZgfCef.Harness/) PASSES** —
google.com + run.world both paint off-screen on our own binding, saved as PNG.
**Phase 5 PASSES** — the `window.zgf` JS↔C# bridge round-trips a message (page→C# and
C#→page→C#), input (mouse move/click/wheel, keyboard, focus) is accepted with no crash,
and page console + load-error + render-process-death are surfaced; verified 8/8 runs.
**Phase 6 PASSES** — an `IWebView`/`IWebViewFactory` façade ([ZgfCef.WebView/](ZgfCef.WebView/))
over the binding, implementing the **real `ZGF.Gui.IWebView`**, driven end-to-end by a headless
demo ([ZgfCef.WebView.Demo/](ZgfCef.WebView.Demo/)) through a separate-helper subprocess
([ZgfCef.WebView.Helper/](ZgfCef.WebView.Helper/)): loads a page, forwards focus/click/keyboard,
round-trips `window.zgf` both ways, and screenshots — 5/5 runs. The **renderer flake was
investigated** (see the Phase 6 outcome): a Chromium-150-OSR `__fastfail` ~1×/load on heavy pages,
unfixable via config, but Chromium self-recovers ~94% and a rate-limited single reload gets paint to
20/20. Next: Phase 7 (CI). Drafted 2026-07-21.
**Location:** the Sandbox repo (`cs_games_sandbox`), directory `Cef150Binding/`. **Not** the ZGF
submodule. Nothing here touches `ZGF/` until Phase 6, and even then only through a demo.

## Handoff — continuing from here

**Where we are:** Phases 0–5 done. The binding is real and proven — google.com and run.world
both render off-screen through our own `Xilium.CefGlue` fork on Chromium 150, **and** the
`window.zgf` JS bridge + input + crash/error surfacing all work (Phase 5). Nothing is
committed yet; all changes are in the working tree under `Cef150Binding/` (ZGF submodule
untouched). The `cef/` payload and `**/bin`,`**/obj` are gitignored.

**Reproduce the current state from a clean checkout:**
1. `./fetch-cef.ps1` — stages `cef/win-x64/{include,Release,Resources}` (needs network; verifies SHA1).
2. `./generator/regen.ps1` — regenerates the interop into `ZgfCef.Interop/` (needs Python 3;
   defaults to `CEF_API_VERSION=15000`). *You normally don't need this — the generated `*.g.cs` are
   already in the tree; only re-run after changing headers/version or the generator.*
3. `dotnet build ZgfCef.Harness` then run `bin/Debug/net10.0/win-x64/zgf-cef-harness.exe` →
   expect `RESULT: PASS` and two PNGs in its `shots/` dir.

**Key facts to carry forward (the ones that bite):**
- **API version is pinned to `15000`** (`CEF_API_VERSION_LAST` for this payload). It shows up in
  three places that must agree: the generator's gate resolution, `version.g.cs`'s `CEF_API_VERSION`
  const, and `CefRuntime.CheckVersionByApiHash`'s `api_hash(CEF_API_VERSION, 0)` call. Bumping CEF =
  re-fetch + re-pin version + `regen.ps1` + redo the **plain-struct/enum audit** (see below).
- **The generator emits vtable structs only — not enums, not plain data structs.** Those are
  hand-maintained. New/changed enums + plain structs per CEF bump go in `Interop/Structs/*.cs`
  (+ `cef_types_150.cs` for new interop enums) and their friendly wrappers in `Structs/*.cs`. The
  versioned API's leading `size_t size` field is the usual culprit for silent `cef_initialize`
  crashes — always diff subset structs against `cef/win-x64/include/internal/cef_types*.h`.
- **`get_capi_name` in the generator matches CEF's naming rule, which changed at 150.** It now inserts
  `_` at digit→uppercase boundaries (`cef_v8_value_t`, `cef_x509_certificate_t`). This drives the
  **exported global function** P/Invoke `EntryPoint` strings, so a wrong rule = `EntryPointNotFound` at
  runtime the moment a global (not vtable) function is called — invisible until V8 (Phase 5). If a
  future CEF bump changes the rule again, this is where to fix it, then delete stale old-name `*.g.cs`
  and re-run the sed over hand-written type/method references.
- **Two ways to drive CEF now.** The Phase-4/5 **harness** is single-exe (`ExecuteProcess` gate,
  `BrowserSubprocessPath = ProcessPath`, render-process `HarnessApp` passed to both `ExecuteProcess` and
  `Initialize`). The Phase-6 **façade** (`ZgfCef.WebView`) is the ZGF-style **separate-helper** model
  (`zgf-cef-helper` exe runs the gate; browser process never does). Both work; the separate-helper is
  the production shape. **The windowless renderer `__fastfail`s ~1×/load on heavy pages** — investigated
  in Phase 6 (Chromium-150-OSR fault, not our ABI), mitigated by rate-limited reload. See Loose ends.

**Next up — Phase 7** (CI: fetch → regen → build → run the smoke test, assert non-blank + no crash;
cache the CEF download by version). Phase 6 (the `IWebView` façade + demo) is **done** — see its
section for the projects and the recorded swap decision. See per-phase sections below.

**Reproduce Phase 6:** `dotnet build ZgfCef.WebView.Demo` (builds the whole chain + stages CEF + the
helper), then run `bin/Debug/net10.0/win-x64/zgf-webview-demo.exe` → expect
`RESULT: PASS … IWebView façade drives our CEF 150 binding end-to-end` and a `webview-demo.png`. Pass
URLs (`zgf-webview-demo https://www.google.com https://run.world`) for the heavy-page flake bench.

**Loose ends / watch-outs:**
- **⭐ Renderer `__fastfail` on heavy OSR pages (investigated, mitigated — Phase 6).** Chromium 150's
  windowless renderer `__fastfail`s (`0xC0000409`) ~1×/load on heavy pages; single-exe also logged
  `Message N rejected by blink.mojom.WidgetHost`. **Ruled out** as a fix: process model, message loop,
  GPU, site isolation, `force-device-scale-factor`, ~8 flags — it reproduces with stock CEF config, so
  it's a Chromium-150 OSR fault (this box is multi-monitor HiDPI), not our ABI. Chromium self-recovers
  ~94%; the façade's rate-limited single reload (`CefWebView` `MaxCrashReloads=1` /
  `MinReloadIntervalSeconds=4`) gets paint to **20/20** without the reload storm the old unconditional
  reload caused. `ZGF_CEF_FLAGS`/`ZGF_CEF_DEBUG` levers stay for a future real fix (newer CEF or driver
  update). The Phase-4/5 harness still uses `OsrClient.MaxCrashReloads=5` (its own smoke-test knob).
- Frame identifiers became opaque strings in CEF 150; the friendly API keeps a best-effort `long`
  shim (`long.TryParse`, default `-1`). Fine for our OSR path (we use `GetMainFrame`), but widen to
  `string` if id round-tripping ever matters.
- `OnAcceleratedPaint` now takes `cef_accelerated_paint_info_t*`; we pass its `shared_texture_handle`
  and otherwise ignore it (software `OnPaint` path only).
- macOS/Linux: the generator is RID-agnostic and `cef_types_150.cs` notes platform specifics, but
  `fetch-cef.ps1` and struct audits are win-x64 only so far.

## Why

`ZGF.WebView.Cef` binds CEF through **CefGlue.Common `120.6099.211`**, which is **Chromium 120**
(Dec 2023) — and CefGlue's own `main` branch is *also* frozen at 120 (`CHROME_VERSION_MAJOR = 120`
in `version.g.cs`). On this build the render process crashes on ordinary open-web pages:

```
[ERROR:shared_image_manager.cc(220)] SharedImageManager::ProduceSkia:
  Trying to Produce a Skia representation from a non-existent mailbox.
→ render process CRASHED on https://www.google.com/
```

This is a Chromium-120-era OSR compositor fault. Prebuilt CEF binaries for **Chromium 150** exist
(Spotify CDN / the `chromiumembeddedframework.runtime.*` NuGets go to 150), but you cannot drop a
150 runtime under the 120 binding — the P/Invoke vtable/struct ABI is generated per CEF version and
must match exactly. So: **regenerate our own binding against CEF 150.**

We already do exactly this shape of work for OpenGL (`OpenGLBindingsGenerator` parses `gl.xml` and
emits C# P/Invoke). CEF is the same idea with a harder input (C headers, ref-counted vtables).

## What CEF actually is (the thing that sizes this project)

CEF is **not** a flat C API like GLFW. Its C API (`include/capi/*.h`) is COM-shaped:

- Every object is a struct beginning with `cef_base_ref_counted_t` (`add_ref` / `release` /
  `has_one_ref`) followed by a **vtable of function pointers**.
- **Two directions.** Objects CEF implements and we *call* (`cef_browser_t`, `cef_frame_t`,
  `cef_v8value_t`…); objects *we* implement and CEF *calls back* into (`cef_client_t`,
  `cef_render_handler_t`, `cef_life_span_handler_t`…).
- Callbacks arrive on CEF's own threads (UI / IO / renderer).
- Struct layout + vtable method order must be **byte-exact** for the CEF version. One added/reordered
  method silently corrupts the vtable → crash. This is why it is generated, and why every Chromium
  bump forces a regeneration.

Getting `add_ref`/`release` right across the managed boundary (keep the C# object pinned via a
`GCHandle` until CEF's refcount drops to zero) is the single biggest source of interop bugs.

## Success criteria (Definition of Done)

1. A standalone Sandbox harness creates an **off-screen** browser on **Chromium 150**, navigates to
   `https://www.google.com` **and** `https://run.world`, and **does not crash the render process** —
   it produces a correct `OnPaint` frame (validated by a saved PNG).
2. Input parity: mouse move/click/wheel, keyboard (the `OnTextInput` char path we already fixed),
   focus, cursor, resize/DPI.
3. JS↔C# bridge parity: `window.zgf.postMessage` (page→C#) and `ExecuteScript`/`PostMessage`
   (C#→page).
4. Crash/error visibility retained: render-process-terminated, load errors, page console errors.
5. Exposed through an **`IWebView`-shaped** surface so it can later replace CefGlue inside
   `ZGF.WebView.Cef` as a package swap, not a rewrite.
6. Reproducible: a GitHub Action fetches the CEF 150 payload, runs the generator, builds, and runs
   the smoke test.

Non-goals for v1: the accelerated shared-texture path, transparent pages, DevTools UI, macOS `.app`
bundling. Windows-x64 first; keep the generator RID-agnostic so linux/macOS follow.

## Guiding principles

- **Validate the hypothesis before building the binding.** Phase 0 proves Chromium 150 OSR actually
  fixes the crash using the cheapest possible probe. If 150 still crashes, we stop — the binding is
  worthless and the fault is in how we drive OSR, not the Chromium version.
- **Generate, don't hand-write.** Reuse CefGlue's generator wherever possible; hand-authoring ~20
  ref-counted vtables by hand is how you get memory-corruption bugs.
- **Bound the surface.** We only need the subset our web view uses (below). Regenerate everything,
  but only *fix* what our subset touches at compile time.
- **Prebuilt binaries, never build Chromium.** Chromium-from-source is a multi-hour / ~100 GB
  ordeal. Spotify ships prebuilt CEF distributions with headers + binaries for every version.
- **Mirror the `IWebView` seam** so the eventual ZGF swap is contained.

## The binding subset we actually need

Enumerated from the current `ZGF.WebView.Cef` usage.

**Handler interfaces we implement (CEF calls in):**
`cef_app_t`, `cef_browser_process_handler_t`, `cef_render_process_handler_t`, `cef_client_t`,
`cef_life_span_handler_t`, `cef_render_handler_t`, `cef_display_handler_t`, `cef_load_handler_t`,
`cef_request_handler_t`, `cef_v8handler_t` (~10).

**Interfaces CEF implements (we call):**
`cef_browser_t`, `cef_browser_host_t`, `cef_frame_t`, `cef_process_message_t`, `cef_list_value_t`,
`cef_v8value_t`, `cef_v8context_t`, `cef_command_line_t`, `cef_request_t` (~9).

**Plain structs (marshaling only, no vtable):**
`cef_settings_t`, `cef_browser_settings_t`, `cef_window_info_t`, `cef_key_event_t`,
`cef_mouse_event_t`, `cef_screen_info_t`, `cef_rect_t`, `cef_color_t`, `cef_string_t` /
`cef_string_utf16_t`, `cef_main_args_t`.

**Global functions (~25–30):**
`cef_execute_process`, `cef_initialize`, `cef_shutdown`, `cef_do_message_loop_work`,
`cef_browser_host_create_browser` / `_sync`, `cef_string_utf16_set` / `_clear`,
`cef_string_userfree_utf16_free`, `cef_process_message_create`, `cef_list_value_create`,
`cef_v8value_create_object` / `_function` / `_string` / `_null` / `_undefined`,
`cef_v8context_get_current_context`, `cef_command_line_get_global`, plus `cef_api_hash` for the
version guard.

→ **~20 vtable interfaces + ~30 functions + ~10 structs.** A few thousand lines of generated interop
plus the ref-count/thread plumbing. Real, bounded — not "a handful of P/Invokes."

## Phases

### ✅ Phase 0 — Prove 150 fixes the crash (½–1 day) — GO/NO-GO GATE — **DONE, GO**
Do this before anything else, using stock CEF (no binding work yet).
- Download the CEF 150 **standard** distribution from the Spotify CDN and run its bundled
  `cefsimple --off-screen-rendering-enabled --url=https://www.google.com` (and `run.world`). This is
  the same CEF 150 payload we will bind, so a clean result here is a real signal.
- **Gate:** renderer survives + paints on both pages → proceed. Still crashes → STOP, reassess
  (our OSR usage, flags, or `windowless_frame_rate`), don't build the binding.
- **Outcome:** used the prebuilt **`cefclient.exe`** from the CEF 150 **client** distro (`cefsimple`
  is windowed-only; `cefclient` is the OSR-capable sample). Both pages survived + painted, no
  `shared_image_manager`/`ProduceSkia` crash. run.world's render process ran the page's own JS
  bundle. Full write-up in [PHASE0-RESULT.md](PHASE0-RESULT.md).

### ✅ Phase 1 — Acquire CEF 150 payload (½ day) — **DONE**
- Source: **Spotify CDN** `https://cef-builds.spotifycdn.com/index.html`. Pick a `cef_binary_150.*`
  build; grab the **minimal** distribution per RID (win64 first). It contains:
  - `include/` — the C API headers (generator input).
  - `Release/` (libcef.dll + ICU/pak deps), `Resources/` (`*.pak`, `locales/`) — the runtime.
- Vendor a pinned copy (or a `fetch-cef.ps1`/`.sh` that downloads + verifies SHA256). Record the
  exact CEF version + Chromium version in a `CEF_VERSION` file.
- Layout: `Cef150Binding/cef/win-x64/{include,Release,Resources}/`.
- **Outcome:** pinned `150.0.14+g7c1aa68+chromium-150.0.7871.129` in [CEF_VERSION](CEF_VERSION);
  [fetch-cef.ps1](fetch-cef.ps1) reads the pin, downloads minimal/client per RID, verifies **SHA1**,
  and stages `cef/win-x64/{include,Release,Resources}` (gitignored). (Note: the CDN publishes SHA1,
  not SHA256 — the script verifies SHA1.)

### ✅ Phase 2 — Stand up the generator (2–4 days, the crux) — **DONE**
- First task: **inspect CefGlue's actual generation tooling** (OutSystems/CefGlue repo). Confirm how
  `Interop/*.g.cs` is produced (CEF's `tools/translator.py` with C# templates vs a bundled
  generator) — the Gustavo fork documents "replace `include/` and run the Python scripts", so it is
  scriptable.
- Fork/vendor CefGlue's generator into `Cef150Binding/generator/`, point it at our
  `cef/win-x64/include`, regenerate the interop (`*.g.cs`). This reuses their proven vtable +
  refcount machinery — the whole point of staying on CefGlue rather than hand-rolling.
- Output: `Cef150Binding/ZgfCef.Interop/` (generated P/Invoke), committed with the generator so it is
  reproducible.
- If the generator needs updates for 120→150 header shape changes, fix the generator (a CefGlue PR
  candidate), not the generated output.
- **Outcome:** the generator is Python (`cefglue_interop_gen.py` + `cef_parser.py`, CefGlue's descent
  of CEF's `translator.py`), vendored to [generator/](generator/) with a `cef150-generator.patch`
  (clean CefGlue-PR candidate), `regen.ps1`, and README. **3 fixes** for 120→150:
  1. **API-version resolution** in `cef_parser.py` (`resolve_api_version`) — the big change. CEF 127+
     gates members with `#if CEF_API_ADDED/REMOVED(v)` and keys the ABI to `CEF_API_VERSION`. The
     new pass resolves gates against a pinned **`CEF_API_VERSION=15000`** (`CEF_API_VERSION_LAST`;
     override via env / `regen.ps1 -ApiVersion`) so vtables are byte-exact — includes numbered
     members ≤15000, excludes the one `CEF_API_ADDED(CEF_EXPERIMENTAL)` member.
  2. **`make_version_cs`** — the API hashes moved out of `cef_api_hash.h` into per-version defines in
     `cef_api_versions.h`; now reads the v15000 WIN/MAC/LINUX hash + emits a `CEF_API_VERSION` const.
  3. **8 new CEF-150 class roles** added to `schema_cef3.py`, classified by each header's `allocated
     client-side` (handler) vs `DLL-side` (proxy) note. (22 removed classes remain as harmless stale
     map entries.)
  Regenerates ~136 interop `*.g.cs`.

### ✅ Phase 3 — Compile the managed binding (2–3 days) — **DONE**
- New project `Cef150Binding/ZgfCef.Interop` (net10.0, `AllowUnsafeBlocks`).
- Make **our subset** compile against the 150-generated code; fix 120→150 API deltas as the compiler
  surfaces them (method added/renamed, handler signature changes — `OnAcceleratedPaint`, IME, and
  `OnBeforeCommandLineProcessing` are the usual movers between CEF releases).
- Implement the interop runtime plumbing the generator doesn't: `GCHandle` pinning for handler
  objects, refcount base, `cef_string_t` marshaling helpers, thread-affinity asserts.
- **Outcome:** [ZgfCef.Interop/](ZgfCef.Interop/) is a **fork of CefGlue's core managed lib**
  (`Xilium.CefGlue`, net10.0 — assembly name kept so the eventual ZGF swap is a reference swap, not a
  rewrite). The refcount/marshaling/`GCHandle` plumbing is CefGlue's proven base (not re-invented).
  Deltas fixed: ~27 hand-wrapper method signatures in `Classes.Handlers/` + `Classes.Proxies/` (new
  callbacks like `on_before_popup_aborted`, `on_render_process_unresponsive`; changed
  `on_before_popup`/`on_render_process_terminated`/`on_accelerated_paint`; removed extension/frame
  members). **Two ABI-critical fixes the compiler could not catch (both crash silently otherwise):**
  - `cef_api_hash` is now 2-arg `(int version, int entry)` — fixed the P/Invoke in `Interop/libcef.cs`
    and the call in `CefRuntime.CheckVersionByApiHash` to pass `libcef.CEF_API_VERSION`.
  - CEF's versioned API prepends a `size_t size` field to many **hand-written plain structs**
    (`cef_screen_info_t`, `cef_popup_features_t`, `cef_key_event_t`, `cef_window_info_t`, …) and
    reshuffles `cef_settings_t` (dropped `chrome_runtime`/`persist_user_preferences`/
    `pack_loading_disabled`; added `log_items`/`disable_signal_handlers`/`use_views_default_popup`).
    All reconciled against the 150 headers in `Interop/Structs/*.cs`; new interop enums/structs live
    in `Interop/Structs/cef_types_150.cs`. **The generator emits neither enums nor plain structs**, so
    every CEF bump needs this manual struct/enum audit.

### ✅ Phase 4 — Minimal OSR harness (2–3 days) — **DONE, GATE PASSED**
- `Cef150Binding/ZgfCef.Harness` console app, **no ZGF dependency**. Mirrors today's bootstrap:
  separate `zgf-cef-helper`-style subprocess (or single-exe `ExecuteProcess` gate for the harness),
  external message pump, windowless browser, `OnPaint` → BGRA→RGBA → PNG.
- **Gate:** google.com + run.world paint without a render-process crash (the Phase 0 result, now on
  our own binding).
- **Outcome:** [ZgfCef.Harness/](ZgfCef.Harness/) uses the **single-exe `ExecuteProcess` gate** model
  (this exe relaunches as its own CEF subprocess; `BrowserSubprocessPath = Environment.ProcessPath`),
  external message pump (main-thread `DoMessageLoopWork` loop), windowless 1280×800 browser, BGRA→RGBA
  `OnPaint` → **zero-dependency PNG encoder** (`Png.cs`, inline zlib/CRC32 — no image lib). The csproj
  `StageCef` target copies `cef/win-x64/{Release,Resources}` next to the exe. **Both pages render
  pixel-perfect off-screen on our own binding** (google.com ~29 paints; run.world ~150 paints, the
  full SPA that crashed CEF 120) → PNGs in the harness `shots/` dir, verified non-blank + visually
  correct. `RESULT: PASS`.

### ✅ Phase 5 — Input + bridge parity (2–3 days) — **DONE, GATE PASSED**
- Port the input translation (reusing the fixed `OnKeyboardKeyStateChanged`/`OnTextInput` char logic)
  and the `window.zgf` V8 bridge (`cef_v8handler_t` + process messages).
- Port crash/error surfacing (`OnRenderProcessTerminated`, `OnLoadError`, `OnConsoleMessage`) — the
  logging we just added.
- **Outcome:** the render-process app ([ZgfCef.Harness/HarnessApp.cs](ZgfCef.Harness/HarnessApp.cs),
  ported from `ZgfCefRenderApp`) installs `window.zgf.postMessage`/`onmessage`; it is handed to **both**
  `ExecuteProcess` and `Initialize` (single-exe model). [OsrClient.cs](ZgfCef.Harness/OsrClient.cs) gained
  the browser side of the bridge (`OnProcessMessageReceived`), display (title/url/cursor/console), load
  (`OnLoadError`), and the 4-arg CEF-150 `OnRenderProcessTerminated`. The harness
  ([Program.cs](ZgfCef.Harness/Program.cs)) exercises it all: a data-URL page posts `hello-from-page`
  (page→C#), the harness fires the full input surface, then `PostMessageToPage("\"ping\"")` and the page
  echoes `echo:ping` back (C#→page→C#). Verified `Phase 5 OK` on 8/8 runs. It also ported ZGF's **bounded
  reload-on-crash recovery** (`OnRenderProcessGone` → `Reload()`).

  **Two ABI fixes the compiler could not catch, both discovered here (V8 was the first code to call CEF
  *global* functions by name):**
  1. **Generator naming rule change for CEF 150 (the crux).** CEF 150's C API inserts a `_` at every
     digit→uppercase boundary: `CefV8Value` → `cef_v8_value_t` (was `cef_v8value_t`),
     `CefX509Certificate` → `cef_x509_certificate_t`. Vtable methods are called by pointer so this was
     invisible through Phase 4, but the **exported global functions** (`cef_v8_value_create_object`, …)
     are P/Invoked by name → `EntryPointNotFoundException` → render process crash. Fixed `get_capi_name`
     in [generator/cef_parser.py](generator/cef_parser.py) (one added condition: underscore when the
     previous char is a digit), regenerated, deleted the 12 stale old-name `*.g.cs`, and renamed the
     ~13 hand-written references (also hit internal method names: `base64encode`→`base64_encode`,
     `get_v8context`→`get_v8_context`, `get_x509certificate`→`get_x509_certificate`,
     `send_http200response`→`send_http200_response`). **This is a clean CefGlue-PR-candidate fix and
     part of the per-bump regen ritual now.**
  2. Bridge V8 lifetime uses CefGlue's proven `GCHandle`/refcount base unchanged — no extra plumbing.

  **⚠️ Pre-existing renderer flake surfaced (NOT a Phase-5 regression):** Chromium 150's windowless
  renderer intermittently `__fastfail`s (`STATUS_STACK_BUFFER_OVERRUN` / `0xC0000409`) during load. With
  `ZGF_CEF_DEBUG=1` (harness now forwards `enable-logging`/`v=1` like ZGF) the renderer logs
  `Message 6/7 rejected by interface blink.mojom.WidgetHost` immediately before dying — the **browser
  rejects a widget IPC message and kills the renderer**. Proven pre-existing: it reproduces with **no
  render app registered** (pure Phase-4 config) and independent of `disable-gpu`, so Phase 4's single
  "PASS" was a lucky run. The harness's `MaxCrashReloads=5` makes the smoke test deterministic while
  logging every death. **Investigated and resolved in Phase 6 — see that section.**

### ✅ Phase 6 — `IWebView` shim + ZGF demo (1–2 days) — **DONE, PASSES**
- Implement an `IWebView`/`IWebViewFactory` façade over the new binding, byte-compatible with the
  interfaces in `ZGF.Gui`. Prove it by pointing a *copy* of the ZGF WebView demo at it from the
  Sandbox (reference the Sandbox binding instead of `ZGF.WebView.Cef`). ZGF source stays untouched.
- **Outcome:** three new projects under `Cef150Binding/`:
  - **[ZgfCef.WebView/](ZgfCef.WebView/)** — the façade. References the **real `ZGF.Gui`** (so
    `CefWebView` implements the real `ZGF.Gui.IWebView` — the byte-compatible seam) and `ZgfCef.Interop`
    (our `Xilium.CefGlue`) instead of `CefGlue.Common`. `CefWebView`/`CefWebViewClient`/`ZgfCefRenderApp`
    are near-verbatim ports of the `ZGF.WebView.Cef` sources with the two CEF-150 signature deltas
    (`OnBeforePopup` gained `popupId`, `OnRenderProcessTerminated` is 4-arg). `CefRuntimeHost` is a
    self-contained, headless-drivable variant (exposes `Pump()` instead of needing an `IUiDispatcher`).
  - **[ZgfCef.WebView.Helper/](ZgfCef.WebView.Helper/)** — the `zgf-cef-helper` subprocess (separate-helper
    model, mirroring `ZGF.WebView.Cef.SubProcess`): links `ZgfCefRenderApp.cs` and runs the
    `ExecuteProcess` gate. So the demo exe is the browser process only.
  - **[ZgfCef.WebView.Demo/](ZgfCef.WebView.Demo/)** — a headless driver (the ZGF demo's `drive` check
    pointed at the Sandbox binding): creates a view via the factory, loads a page, forwards
    focus/click/keyboard, types `ZGF` and reads it back (`val:ZGF`), round-trips `window.zgf` both ways
    (`echo:ping`), and saves a PNG. **5/5 PASS.** Also has a URL mode (`zgf-webview-demo <url>…`) that
    was the flake test bench. Its csproj stages the CEF payload + the helper (into `cef-helper/`).
- **Swap decision — recorded AND executed (2026-07-21).** The production swap is a **reference swap
  inside `ZGF.WebView.Cef`**, no sibling engine (the façade proved the surface is identical). Now done:
  `ZGF.WebView.Cef.csproj` + `ZGF.WebView.Cef.SubProcess.csproj` reference
  `Cef150Binding/ZgfCef.Interop` instead of `CefGlue.Common` (assembly stays `Xilium.CefGlue`, both ZGF
  projects only used the core API); the 2 handler deltas are applied to `CefWebViewClient`
  (`OnBeforePopup` +`popupId`, `OnRenderProcessTerminated` 4-arg); the rate-limited reload fix +
  Chromium-150 message are applied to `CefWebView`; and `ZGF.WebView.Cef.targets` now stages the pinned
  CEF 150 payload (`Cef150Binding/cef/win-x64/{Release,Resources}`) into the app root as transitive
  content (CefGlue.Common's runtime NuGet no longer does). **`ZGF.WebView.Demo` — the real GUI
  mini-browser — now renders google.com on Chromium 150** (was blank on 120): verified via
  `ZGF.WebView.Demo.exe https://www.google.com --screenshot`. This is the first change to the ZGF
  submodule; `CefGlue.Common` `PackageVersion` entries in `ZGF/Directory.Packages.props` are now unused
  (left in place, harmless). Real-swap follow-ups if this ships: package `ZgfCef.Interop` as a NuGet
  instead of a cross-submodule `ProjectReference`; add win-arm64/osx/linux payloads.

- **🔎 Renderer flake — investigated and resolved (mitigated).** The Phase-5 `__fastfail`
  (`0xC0000409`) was chased down here. **Ruled out** (all still ~1 crash/load): the process model
  (single-exe vs separate-helper — built Phase 6 on separate-helper, no change), the message loop
  (`ExternalMessagePump` vs `MultiThreadedMessageLoop`), `disable-gpu`/`disable-gpu-compositing`, site
  isolation (`disable-site-isolation-trials` + `disable-features=IsolateOrigins,site-per-process`),
  `force-device-scale-factor=1`, and ~5 other flags. It reproduces with **stock CEF config**, so it is
  a Chromium-150 windowless-OSR fault (this box is multi-monitor HiDPI), **not** our binding's ABI —
  consistent with Phase 0's `cefclient` also "surviving + painting" rather than being crash-free.
  **Key finding:** the crash fires *before* first paint, yet **Chromium relaunches the renderer on its
  own and the page still paints** (~94% of loads with *no* reload at all). Our unconditional
  `Reload()`-on-termination was *fighting* that recovery and amplifying one crash into a
  reload→crash→reload storm (measured ~2 deaths/load, no better paint). **Fix:** rate-limited,
  single-shot recovery in `CefWebView.OnRenderProcessGone` (`MaxCrashReloads=1` +
  `MinReloadIntervalSeconds=4`) — one backstop reload for the ~6% Chromium doesn't self-recover, and
  rapid re-crashes are left to Chromium. Result: **20/20 page-loads paint** (google.com + run.world)
  with far less crash noise. Every death is still logged; `ZGF_CEF_DEBUG=1` forwards Chromium verbose
  logging and `ZGF_CEF_FLAGS="…"` remains as a no-rebuild flag lever for a future real fix (a newer CEF
  build or GPU-driver update is the likely eventual cure).

### Phase 7 — CI + packaging (1–2 days)
- GitHub Action (`.github/workflows/cef-binding.yml` in the Sandbox): `fetch-cef` → run generator →
  build `ZgfCef.Interop` + harness → run the smoke test (headless OSR screenshot, assert non-blank
  and no crash log). Cache the CEF download by version hash.
- Stage runtime binaries next to the harness the way `ZGF.WebView.Cef.targets` stages the helper.

## Proposed directory layout (Sandbox)

```
Cef150Binding/
  PLAN.md                     ← this file
  CEF_VERSION                 ← pinned cef+chromium version
  fetch-cef.ps1 / fetch-cef.sh
  cef/win-x64/{include,Release,Resources}/   ← Phase 1 (gitignored or LFS; fetched in CI)
  generator/                  ← Phase 2 (forked/own generator)
  ZgfCef.Interop/             ← Phase 3 (generated P/Invoke + runtime plumbing)
  ZgfCef.Harness/             ← Phase 4–5 (OSR + input + bridge smoke test, no ZGF)
  ZgfCef.WebView/             ← Phase 6 (IWebView façade over the binding; refs real ZGF.Gui)
  ZgfCef.WebView.Helper/      ← Phase 6 (zgf-cef-helper subprocess; ExecuteProcess gate)
  ZgfCef.WebView.Demo/        ← Phase 6 (headless IWebView driver + flake test bench)
  .github/workflows/...       ← Phase 7 (or the Sandbox's existing workflows dir)
```

## Key decisions

| Decision | Choice | Rationale |
|---|---|---|
| Validate before build | Phase 0 spike is a hard gate | A binding is worthless if 150 doesn't fix the crash |
| Binary source | Spotify CDN minimal distro (headers + runtime together) | One source for the exact headers *and* matching binaries |
| Binding | **Fork CefGlue + regenerate for CEF 150** | Reuse its proven refcount/vtable generator; regeneration is the recurring cost. No hand-rolled binding, no CefSharp. |
| Scope | Only the enumerated subset compiles/validated | Bounds the 120→150 delta-fixing to what we use |
| Isolation | All work in Sandbox; ZGF untouched until a demo swap | Keeps the framework releasable while this bakes |
| Platform | win-x64 first, generator stays RID-agnostic | Fastest path to the go/no-go answer |

## Risks

- **150 doesn't fix the crash** → Phase 0 catches it cheaply before investment.
- **Generator is hard to resurrect** → worst case, fix CefGlue's generator for the 120→150 header
  deltas (it is scriptable per the fork's docs); `OpenGLBindingsGenerator` is the in-house precedent
  for parsing a spec into C# if it ever comes to that.
- **Refcount/threading bugs** → the classic interop trap; mitigate by copying CefGlue's base rather
  than inventing it, and by asserting thread affinity in debug.
- **Maintenance treadmill** → every Chromium bump = re-fetch headers + regen + fix deltas. The CI
  action makes this a button, not a project, but the delta-fixing is manual.
- **Cross-platform later** → macOS needs the signed `.app`/helper bundle layout (heaviest task,
  already noted in the ZGF web-view plan); Linux is mostly packaging.

## First actions when we start

1. Phase 0 probe with stock `cefsimple` from a CEF 150 standard distro → confirm google.com +
   run.world survive OSR on 150.
2. If green: `fetch-cef.ps1` pulling a pinned CEF 150 win64 minimal build from the Spotify CDN.
3. Clone OutSystems/CefGlue, locate the generator, do a dry-run regen against the 150 headers.
