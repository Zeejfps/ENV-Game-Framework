# Native Web View

Status: **plan** (not yet implemented). Drafted 2026-07-21.

Embeds a fully-interactive native web view into ZGF, rendered **off-screen into a texture**
and drawn through the ordinary `ICanvas` image path so it composes with the rest of the UI
(clipping, opacity, scale, z-order, scroll panes). Backed by **CEF** (Chromium Embedded
Framework) in off-screen-rendering (OSR) mode, with a two-way JavaScript↔C# bridge.

## Decisions

| Axis | Choice | Consequence |
|---|---|---|
| Compositing | **Off-screen → texture** (not native overlay) | Composes fully with the ZGF canvas; harder path; requires an engine with real OSR. |
| Engine | **CEF** via the maintained **CefGlue** .NET binding (not CefSharp — Windows-only) | One managed implementation for all OSes; heavy native payload. |
| Platforms | **Windows first**, then Linux, then macOS | macOS uses the Metal canvas path (already abstracted by `ICanvas`) + an `.app` bundle. |
| Interactivity | **Full** input + navigation, **with** a JS↔C# bridge | Largest fiddly surface is input/IME forwarding; bridge via CEF message router. |
| Packaging | **Standalone opt-in project**, isolated by the reference graph | Apps that don't use it pay zero bytes / zero build cost. |
| Bootstrap | **Separate subprocess exe** (`zgf-cef-helper`) | No CEF gate in the app's `Main`; the whole lifecycle becomes a hosted service. |

### Why texture, not native overlay

A native overlay (an OS web-view control parented onto the window's `NativeHandle`) is far
less work and has perfect web fidelity, but it floats *above* the GL/Metal surface: ZGF
cannot draw over it, clip it to rounded corners, apply `PushOpacity`/`PushScale`, or
z-order anything on top — the classic "airspace" problem. The texture path avoids all of
that. It's just an `Image`, so every existing canvas facility works on it. The cost is that
only a purpose-built OSR engine gives clean off-screen pixels on all three OSes — which is
why the engine is CEF rather than per-platform WebView2/WKWebView/WebKitGTK.

### Why CEF collapses the "one impl per platform" idea

The original mental model was an `IWebView` seam with a Windows/macOS/Linux implementation
each, like `IClipboard`. With CEF that collapses to **a single managed implementation** —
CEF's own native binaries handle the per-OS differences internally. There is no
`Win32WebView`/`OsxWebView`/`LinuxWebView` split. The per-platform pain moves out of our C#
and into **CEF packaging** (native binaries, and the macOS `.app`/helper layout).

## What already exists (nothing new needed here)

- **Texture-push path:** `ICanvas.CreateOrUpdateRgbaImage(imageId, w, h, rgbaTopDown)`
  (`ZGF.Gui/ICanvas.cs`) uploads raw RGBA into a GPU texture; both backends implement it
  (`GlImageManager.CreateOrUpdateRgbaImage`, and the Metal equivalent). A `WebView` widget
  reuses this exactly like `ImageView` does — no new GPU code on the GUI side.
- **Per-platform seam precedent:** `IClipboard` (interface in `ZGF.Gui`, impls under
  `ZGF.Gui.Desktop/Platforms/*`, selected in `GuiApp.CreatePlatformClipboard`, resolved by
  widgets from the per-window `Context`). The web view follows the *interface-in-core,
  resolved-from-Context* half of this, but not the per-OS-impl half (CEF handles that).
- **Host integration hooks** on `GuiApp`:
  - `app.OnTick` (`GuiApp.cs`) — pump CEF's message loop once per tick.
  - `IUiDispatcher` (the `_dispatcher`) — marshal CEF's cross-thread callbacks onto the UI thread.
  - `startup` / `RegisterServices` DI seams — register the web-view factory.
  - `RequestRedraw()` — drive a repaint when a page frame is ready.
- **Native handle** (`ZGF.Desktop/IWindow.NativeHandle`) — available if a native-overlay
  fallback is ever wanted; unused by the texture path.

## Packaging — the isolation guarantee

ZGF is consumed by downstream apps via **`<ProjectReference>` from the git submodule** (not
published NuGet; MinVer versions the projects but only tests/PngSharp carry package
identity). CEF's native binaries (~150–250 MB, pulled transitively from the `Chromium.Cef`
runtime NuGets) are copied into an app's output **if and only if** something in that app's
reference chain references the CEF project.

**The rule that guarantees "no penalty if you don't use it":**

> No project in the common graph — `ZGF.Gui`, `ZGF.Gui.Desktop`, `ZGF.Desktop`, the
> backends — may reference `ZGF.WebView.Cef`. Only the **app's own csproj** references it,
> and only when it wants a web view.

A downstream app that references `ZGF.Gui.Desktop` and never adds `ZGF.WebView.Cef` gets
zero Chromium bytes, zero CEF assemblies, and unchanged build time.

### Where each type lives (forced by that rule)

| Type | Project | Rationale |
|---|---|---|
| `IWebView`, `IWebViewFactory` | `ZGF.Gui` (core) | Pure interfaces, same home as `IClipboard`. Tiny, inert, no CEF. |
| `WebView` widget / `WebViewView` | `ZGF.Gui` (core) | Texture-based and engine-agnostic; resolves `IWebViewFactory` from `Context` at runtime. App code writes `new WebView { Url = … }` **without referencing CEF**. |
| CEF binding, OSR render handler, input/IME translation, JS bridge, browser-process init/pump/shutdown, shared `CefApp` render handler, **native binaries** | `ZGF.WebView.Cef` (new, opt-in) | The only thing that drags in Chromium. |
| CEF subprocess entry point (`zgf-cef-helper` exe) | `ZGF.WebView.Cef.SubProcess` (new, opt-in) | Separately-launchable helper exe CEF spawns for its render/GPU/utility processes — see [Bootstrap](#bootstrap-the-subprocess-model). References the lib for the shared render-process handler. |

The widget resolving its factory through `Context` — exactly how `TextInput` resolves
`IClipboard` and tolerates its absence (`TextInput.cs`) — is what lets the widget live in
core without pulling CEF. With no factory registered, `WebView` renders a visible
placeholder / clear "did you call `UseCefWebView()`?" diagnostic rather than crashing.
Swapping CEF for another OSR engine (e.g. Ultralight) later is a package swap, not an app
rewrite.

### Opt-in wiring (the only CEF-referencing code an app writes)

With the separate subprocess helper (the chosen bootstrap model, below), the app writes
**nothing** in `Main` and nothing in its csproj beyond a single `ProjectReference` to
`ZGF.WebView.Cef`. All CEF lifecycle lives in a hosted service registered by `UseCefWebView()`:

```csharp
// No CEF gate in Main. The only CEF-referencing symbol an app touches is UseCefWebView().
using var app = GuiApp.CreateBuilder(config)
    .UseCefWebView()                    // ZGF.WebView.Cef: registers IWebViewFactory + a CefHostedService
    .Build(new WebView { Url = "https://example.com" });   // widget from core ZGF.Gui
```

`UseCefWebView()` registers `IWebViewFactory` and a `CefHostedService : IHostedService`
whose `Start()` runs `CefRuntime.Initialize` (pointed at the helper exe), whose per-tick pump
hangs off `app.OnTick`, and whose disposal calls `CefRuntime.Shutdown`. This is possible
*only* because the subprocess gate has been moved out of `Main` — see below.

### Public API (the seam)

```csharp
// ZGF.Gui — no CEF reference
public interface IWebView : IDisposable
{
    void Navigate(string url);
    void LoadHtml(string html, string baseUrl = "about:blank");
    void ExecuteScript(string js);              // C# → page
    event Action<string> MessageReceived;        // page → C# (bridge)
    void PostMessage(string json);               // C# → page (bridge)
    event Action<string> TitleChanged;
    event Action<string> UrlChanged;
    void GoBack(); void GoForward(); void Reload();
}

public interface IWebViewFactory { IWebView Create(); }
```

The retained `WebView` widget lays out like `Image`, owns an `IWebView`, and each frame
draws the latest page pixels via `CreateOrUpdateRgbaImage` + `DrawImage`. Because it's just
an image, `PushClip` (rounded corners), `PushOpacity`, `PushScale`, z-order and nesting
inside a `VerticalScrollPane` all work for free — the entire payoff of the texture path.

### Packaging decisions still open

1. **Solution membership:** keep `ZGF.WebView.Cef` in `ENV Game Framework.sln` (so
   solution builds restore CEF for CI/tests) but referenced by no common project — or keep
   it fully out of the default solution (like `ZGF.Gui.iOS.SmokeTest` already is) so a
   normal `dotnet build` never restores Chromium. Leaning **in-solution but unreferenced**,
   with a CI note.
2. **Future NuGet publish** (not done today): the same split becomes `ZGF.WebView.Cef` as a
   separate package with the natives as RID-specific runtime assets; the interfaces stay in
   the `ZGF.Gui` package. Keeping the abstraction/impl boundary package-clean now makes a
   future publish trivial.

## Bootstrap: the subprocess model

The "init one-liner" is really **two CEF calls with opposite timing rules**, and that
distinction drives the whole bootstrap design.

```csharp
int code = CefRuntime.ExecuteProcess(mainArgs, cefApp, IntPtr.Zero); // (1) subprocess gate
if (code >= 0) return code;                                          //     "I'm a subprocess" → exit
CefRuntime.Initialize(mainArgs, settings, cefApp, IntPtr.Zero);      // (2) main-process init
```

- **(1) `ExecuteProcess` is a `Main`-level gate that cannot be a hosted service.** Chromium
  spawns its render/GPU/utility processes by **re-launching your own exe** with special args,
  so `Main` runs many times per session. `ExecuteProcess` detects a subprocess launch, runs
  Chromium's logic, and returns an exit code so you `return` immediately — a subprocess must
  never reach GLFW, a window, or `GuiApp`. A `HostedService.Start()` runs *after* the window
  is built (`StartHostedServices()` is called in the `GuiApp` ctor, post-`MountContent`), which
  is far too late — the subprocess would already have booted the GUI. No abstraction removes
  this from `Main` **in single-exe mode**.
- **(2) `Initialize` runs only in the real app process**, on the main thread, before the first
  browser is created. Late (post-window) is fine, so it *can* live in a hosted service.

**The chosen model eliminates the `Main` gate entirely** by shipping a dedicated helper exe.
Set `CefSettings.BrowserSubprocessPath` to it, and CEF launches *that* for subprocesses instead
of re-launching the app — so there is no `ExecuteProcess` gate in the app's `Main`, and the
full bootstrap (`Initialize` + per-tick pump + `Shutdown`) becomes a self-contained
`CefHostedService`.

| | Single exe | **Separate subprocess exe (chosen)** |
|---|---|---|
| `ExecuteProcess` gate in `Main` | required (can't be hosted) | **none** |
| `Initialize` + pump + shutdown | hosted service (or in `Main`) | **fully a hosted service** |
| App author writes in `Main` | one-line gate | **nothing** |
| Cost | none | extra build artifact + copy step |

Single-exe stays available as a fallback for a bare-bones app that would rather have the
one-line gate than the extra artifact.

### The helper project

A tiny `OutputType=Exe` project whose `Main` is only the subprocess gate — but it also hosts
the **renderer side of the JS bridge** (`CefMessageRouterRendererSide`), which runs in the
render process, not the app. That handler is defined once in `ZGF.WebView.Cef` and shared:

```csharp
// ZGF.WebView.Cef.SubProcess/Program.cs — the whole program
static int Main(string[] args)
{
    var mainArgs = new CefMainArgs(args);
    var app = new ZgfCefRenderApp();  // shared type in ZGF.WebView.Cef: GetRenderProcessHandler() → router renderer side
    return CefRuntime.ExecuteProcess(mainArgs, app, IntPtr.Zero);
}
```

```xml
<!-- ZGF.WebView.Cef.SubProcess.csproj -->
<PropertyGroup>
  <OutputType>Exe</OutputType>
  <AssemblyName>zgf-cef-helper</AssemblyName>   <!-- the exe CEF launches -->
</PropertyGroup>
<ItemGroup>
  <ProjectReference Include="..\ZGF.WebView.Cef\ZGF.WebView.Cef.csproj" />
</ItemGroup>
```

### Shipping it next to the app

Because apps consume ZGF by `<ProjectReference>`, `ZGF.WebView.Cef` ships a `.targets`
(imported automatically by anything referencing it) that builds the helper and copies its
output into the consuming app's `bin/…/cef-helper/`. The app author does nothing:

```xml
<!-- ZGF.WebView.Cef/build/ZGF.WebView.Cef.targets -->
<ItemGroup>
  <ProjectReference Include="$(MSBuildThisFileDirectory)..\..\ZGF.WebView.Cef.SubProcess\ZGF.WebView.Cef.SubProcess.csproj">
    <ReferenceOutputAssembly>false</ReferenceOutputAssembly>   <!-- build it, don't link it -->
    <Private>false</Private>
  </ProjectReference>
</ItemGroup>
<Target Name="CopyCefHelper" AfterTargets="Build">
  <!-- glob the helper's OutDir → copy into $(OutDir)cef-helper\ -->
</Target>
```

`BrowserSubprocessPath` then points at
`Path.Combine(AppContext.BaseDirectory, "cef-helper", "zgf-cef-helper.exe")`.

### Deployment wrinkles

- **The helper is a .NET exe**, so its output is `zgf-cef-helper.exe` (apphost) + `.dll` +
  `.runtimeconfig.json` + `.deps.json`. Its deployment model must match the app's
  (framework-dependent app ⇒ framework-dependent helper; self-contained/single-file ⇒ the
  helper needs the runtime too). Keep the same TFM/RID and share the app's `bin` tree so it
  resolves the shared runtime.
- **CEF's native binaries are shared, not duplicated.** `libcef`/resources/locales sit in the
  app root from the `Chromium.Cef` runtime package; the helper finds them via the main module
  dir (or `ResourcesDirPath`/`LocalesDirPath`). The `cef-helper/` folder holds only the managed
  helper, not another copy of Chromium.
- **On macOS the helper is mandatory, not optional.** A `.app` bundle must contain
  `…Helper.app`, `…Helper (GPU).app`, `…Helper (Renderer).app` under `Contents/Frameworks/`,
  each with its own `Info.plist`/entitlements, with `BrowserSubprocessPath` pointing into them.
  Building the helper now is the same exe wearing platform-specific bundle clothing later.

## The seven implementation pieces

1. **Process bootstrap — via a separate subprocess exe** (see the dedicated section
   [below](#bootstrap-the-subprocess-model)). Chromium is multi-process; the bootstrap is
   really *two* CEF calls with opposite timing rules, and moving one of them out of `Main`
   is what lets the rest become a hosted service.

2. **Message-loop integration.** Enable external message pumping and call
   `CefRuntime.DoMessageLoopWork()` once per `app.OnTick`, so CEF's browser-UI-thread
   callbacks (including `OnPaint`) fire on ZGF's main thread — sidestepping most GL-context
   threading pain. Later: wire CEF's `OnScheduleMessagePumpWork` to `app.Wake`/`RequestRedraw`
   to pump only when there's work.

3. **Paint → texture.** Windowless rendering via `CefWindowInfo.SetAsWindowless`. Implement
   `CefRenderHandler`:
   - `GetViewRect` → the widget's current logical size.
   - `OnPaint(type, dirtyRects, buffer, w, h)` → CEF hands a **BGRA, top-left-origin**
     buffer. Copy into a double-buffered managed array (CEF may reuse its buffer), swizzle
     BGRA→RGBA, set dirty + `RequestRedraw()`.
   - In the widget's draw, if dirty, call `CreateOrUpdateRgbaImage` (top-down input matches
     CEF's origin; the manager flips internally).
   - **Gotchas to verify:** premultiplied alpha on transparent pages; dirty-rect uploads vs
     full-buffer upload. Start with an **opaque page background** to dodge alpha issues.
   - **Perf upgrade path (later):** CEF accelerated OSR yields a GPU shared-texture handle
     (D3D11 on Windows) that interops straight into GL/Metal, skipping the CPU copy. Note it,
     don't build it first.

4. **Input / focus / IME / cursor.** Translate ZGF input into CEF host calls:
   `SendMouseMoveEvent`, `SendMouseClickEvent`, `SendMouseWheelEvent`, `SendKeyEvent`,
   `SendFocusEvent` (coordinates are widget-local logical points). IME rides ZGF's existing
   `ImeCoordinator`/preedit path into `ImeSetComposition`/`ImeCommitText`.
   `CefRenderHandler.OnCursorChange` feeds `IWindow.SetCursor`. This is the largest fiddly
   chunk and where most bugs will live.

5. **DPI + resize.** Report `canvas.DpiScale` through `CefRenderHandler.GetScreenInfo`
   (device scale factor) for crisp HiDPI text; call `browser.Host.WasResized()` when the
   widget's laid-out size changes so CEF re-queries `GetViewRect` and repaints.

6. **JS↔C# bridge.** CEF's built-in message router (`CefMessageRouterBrowserSide` + a small
   render-process handler): page calls `window.cefQuery({ request, onSuccess, onFailure })`
   → `IWebView.MessageReceived`; C#→page via `ExecuteScript` / frame process message →
   `IWebView.PostMessage`. Gives request/response, not just fire-and-forget.

7. **Lifecycle & multi-window.** Browser creation is async (`CefLifeSpanHandler.OnAfterCreated`);
   queue Navigate/Execute calls until ready. On widget unmount, `CloseBrowser(true)` and
   `RemoveImage` the texture. Marshal any off-main-thread CEF callbacks through
   `IUiDispatcher`. Each `WebView` widget = one CEF browser, all sharing one `CefRuntime`.
   Verify interaction with ZGF's secondary/popup windows and the existing screenshot path.

## Cross-platform packaging cost (the real "all three OSes" work)

Not in the C# — in distribution:

- CEF native binaries per RID (`win-x64`, `linux-x64`, `osx-x64`, `osx-arm64`) via the
  `Chromium.Cef` runtime packages, copied next to the app.
- macOS requires a specific **`.app` bundle layout** (helper apps with entitlements,
  framework under `Frameworks/`) — CEF will not run as a loose exe. This is the single
  biggest macOS-specific task.
- App size grows by hundreds of MB; installer/CI implications.
- Chromium BSD license/attribution to track.

## Phasing

1. **Spike (Windows, static page):** stand up `ZGF.WebView.Cef` + the `zgf-cef-helper`
   subprocess project and its copy `.targets`, init CEF via the `CefHostedService`, OSR
   `OnPaint` → texture → visible in a ZGF `WebView` rendering `example.com`. Proves the
   pipeline and the subprocess bootstrap end to end; no input.
2. **Interactivity (Windows):** mouse, keyboard, scroll, focus, cursor, resize, DPI.
3. **JS bridge (Windows):** message router both directions.
4. **IME + polish:** preedit, in-page clipboard, context-menu decision.
5. **Linux:** mostly packaging + input-quirk shakeout.
6. **macOS:** Metal texture path (already abstracted) + `.app` bundle / helper work.
7. **Perf:** accelerated shared-texture OSR, dirty-rect uploads.

## Decisions (researched)

The questions raised during design, each with the chosen answer and rationale. "v1" = the
Windows-first milestone; several are deliberately scoped down with a documented upgrade path.

1. **Bootstrap.** *Separate subprocess exe* (`zgf-cef-helper`) — no CEF gate in the app's
   `Main`; lifecycle is a `CefHostedService`. Single-exe kept as fallback. See
   [Bootstrap: the subprocess model](#bootstrap-the-subprocess-model).

2. **Binding & versions.** *OutSystems **CefGlue** (`CefGlue.Common`)* — the only maintained,
   cross-platform, OSR-capable .NET CEF binding (CefSharp is Windows-only; Xilium/Litem forks
   are dead). Native binaries flow in automatically via the `chromiumembeddedframework.runtime.*`
   / `cef.redist.*` per-RID NuGet packages — no manual Spotify-CDN fetch. **Caveat (risk):** the
   current `CefGlue.Common` (120.6099.x, Mar 2025) ships **Chromium 120 (Dec 2023)**, far behind
   upstream stable (~143). Acceptable because our content is app-controlled/trusted (decision 8),
   but tracked as a security risk — newer Chromium means supplying our own CEF build.

3. **Distribution.** *Bundle* the Minimal CEF distribution via the runtime NuGet packages for
   v1 — reproducible, offline, simplest CI. Keep the library's CEF-path resolution indirected
   (a `libcef` locator) so **download-on-first-run** can be dropped in later without touching
   call sites; revisit only if base app size becomes a real problem. Per-RID on-disk footprint
   is ~150–250 MB (Linux larger), so this only ever lands in apps that opt in.

4. **Concurrent web views.** *Support many*, one shared `CefRuntime`, one browser per `WebView`
   widget. No artificial cap — the architecture doesn't need one — but each browser carries a
   render process, so memory is documented and `windowless_frame_rate` (decision 12) bounds
   cost. Test with 1–2 in v1.

5. **Transparency.** *Opaque page background for v1.* Set `CefBrowserSettings.BackgroundColor`
   to opaque, which sidesteps CEF's **premultiplied-alpha** `OnPaint` buffer entirely (BGRA,
   premultiplied when transparent — the classic halo bug). Transparent compositing is a later
   feature (needs a premultiplied-aware image draw path in the canvas shader).

6. **Content source & navigation.** *App-controlled content*, not a general-purpose browser:
   a custom **`app://` scheme handler** serves bundled local HTML/assets, plus an optional
   per-`WebView` remote-URL **allowlist** (default-deny navigation outside it via
   `OnBeforeBrowse`). This is also what makes the Chromium-120 lag tolerable — we're not
   pointing it at the open web by default.

7. **Session / profile.** *Persistent by default* — `CefSettings.RootCachePath` under the app's
   data dir (cookies/cache/localStorage survive restarts, needed for login flows). Per-view
   **ephemeral/incognito** available via a separate request context.

8. **Web view in secondary/popup windows.** *Main window only in v1*, documented as a
   simplification — **not** a hard limit: secondary/popup windows share the main GL context
   (`OpenGlApp.cs` creates them with the main window as the share source), so the shared
   `GlImageManager` texture is already visible everywhere; only upload-context and per-window
   input routing need care. Deferred, not blocked.

9. **Popups / dropdowns / new windows.** v1 *implements `PET_POPUP`* handling (`OnPopupShow`/
   `OnPopupSize` + `OnPaint` popup element) so HTML `<select>` dropdowns work — expected UX,
   cheap. `window.open`/`target=_blank` → `OnBeforePopup` returns **true (cancel)**; the app
   gets an event to decide (navigate in place / open elsewhere). No native popup browsers. JS
   `alert`/`confirm` routed through a dialog handler → mapped to ZGF dialogs later; auto-handled
   in v1.

10. **Focus & scroll arbitration.** The `WebView` participates in ZGF's focus system; while
    focused, keyboard/IME route to CEF through the existing `ImeCoordinator` seam. **Wheel over
    a focused/hovered web view scrolls the page** and is consumed (does not bubble to an
    enclosing `ScrollPane`) — documented rule; nested-scroll niceties deferred.

11. **NativeAOT / trimming.** *Out of scope for web-view apps in v1.* CefGlue is not
    AOT-verified, leans on `System.Text.Json` reflection, and CEF's multi-process model needs a
    real on-disk helper exe (fights single-file). Apps using the web view publish
    framework-dependent or self-contained (non-single-file). **Core ZGF stays AOT-clean** because
    the web view is opt-in and out of the common graph.

12. **DevTools & repaint cadence.** Expose `IWebView.ShowDevTools()`; v1 uses CEF's
    `--remote-debugging-port` (open in the system browser) — trivial; native-window DevTools
    later. Repaint: `windowless_frame_rate` **30** (CEF default; max 60), and drive ZGF redraws
    from `OnPaint` → `RequestRedraw`, preserving ZGF's idle-friendly on-demand loop. Optional
    `external_begin_frame` for vsync alignment later.

13. **Submodule.** The new `ZGF.WebView.Cef` and `ZGF.WebView.Cef.SubProcess` projects and the
    builder changes land in this repo (`ENV-Game-Framework`).

## Risks tracked

- **Chromium 120 lag** in `CefGlue.Common` (decision 2) — mitigated by app-controlled content
  (decision 6); escalates to "build our own CEF" if arbitrary-web or current-Chromium is required.
- **App size** +150–250 MB per RID for opt-in apps (decision 3) — download-on-first-run is the escape hatch.
- **macOS packaging** is the heaviest platform task — 5 helper `.app` bundles under
  `Contents/Frameworks/`, each signed separately with Hardened-Runtime entitlements
  (`com.apple.security.cs.allow-jit`, `allow-unsigned-executable-memory`,
  **`disable-library-validation`** — mandatory, the framework loads dynamically), signed
  inside-out, then notarized. Reference: the Grayjay.Desktop .NET+CEF entitlements scripts.

## To confirm against the exact CEF build before coding

- Pin the `CefGlue.Common` version and the matching `chromiumembeddedframework.runtime.*` /
  `cef.redist.*` per-RID packages; note the Chromium version they carry.
- `OnAcceleratedPaint` signature and IME method signatures have shifted across CEF releases —
  verify against the shipped build before the perf phase (decision 12 / piece 3 upgrade path).
- Validate the macOS helper/entitlements recipe against that build before the macOS phase.
