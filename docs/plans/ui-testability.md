# UI Testability: Scriptable, Headless, Cross-Platform

Status: **phases 1–4 implemented**; D8 remains. Drafted 2026-11-08. Usage is in `docs/testing.md`.
Not yet run on macOS: the Metal device, offscreen targets and automation-mode setup compile but have
only been verified on Windows and Linux (Mesa).

## Goal

An agent working on a change can **see and drive the UI it just changed**, on whatever machine it is
running on (Windows, Linux or macOS), without taking over the user's mouse, keyboard or focus, and
with screenshots drawn by the shipping GPU renderer. It does this from a short C# script it writes
and runs in seconds. MCP stays, as the out-of-process transport and for poking at an app that is
already running.

This is a development-loop tool first. Scripts that agents write this way can be kept as regression
tests, but CI does not drive these decisions.

### The loop an agent runs

```csharp
[Fact]
public void NewFile()
{
    using var app = GpuFrames.Running(_device, new MyApp(), width: 900, height: 600);  // returns once fonts and posted work are done
    app.ClickOn("new-file");
    app.Type("notes.md");
    app.Key(KeyboardKey.Enter);
    app.Settle();
    app.Look("after-new-file");   // scratch/after-new-file.png + .txt, both paths printed
}
```

```
dotnet test ZGF.Gui.Immediate.Scratch --filter NewFile
```

The agent reads the text first (cheap and exact) and opens the PNG only when the question is visual.
The first phase delivers exactly this; everything else is improvement on top.

## Where things stand

| | Retained (`ZGF.Gui`) | Immediate (`ZGF.Gui.Immediate`) |
|---|---|---|
| Headless harness | `GuiTestHarness` | `UiFrames`: synthetic clock; coordinate, drag, key-up/down and IME input; `ClickOn("id")` |
| Semantic snapshot (role/label/state) | yes, from `View.Accessibility` | **no**: an `Element` carries a hashed `int` id and text |
| Live-app driver | `GuiDriver` | **none** |
| MCP `gui_*` tools | yes | **none** |
| Screenshot | `GuiDriver` → render backend | `ImmediateGuiAppHost.RequestScreenshot` is public, but no driver or tool exposes it |
| Pixels without a GPU | `RasterCanvas` (`GuiTestHarness.CreateRaster`) | `RasterFrames` (same `RasterCanvas`) |
| Agent guidance | `ZGF.Gui.Testing/README.md` | **none**; nothing says which harness or tool to reach for |

Specific problems:

1. **`RasterCanvas` is a third renderer that does not match the other two.** It shares staging,
   clipping and shaping with the GPU canvases but re-implements ~830 lines of GLSL in ~200 lines of
   C#: no box shadows, no transform edge fading, no rounded image corners, no image rotation, no
   dashing or gradients, unantialiased rect corners, nearest-sampled glyphs and images. Border-side
   priority is top > bottom > left > right (`RasterCanvas.cs:186`) while `canvas_rect.frag.glsl` is
   bottom > top > right > left, and the shader's comment claims they match.
2. **Immediate-mode apps cannot be seen or driven from outside the process.**
3. **The live immediate host reads the real OS.** Pointer moves are dropped unless the OS cursor is
   over the window (`IsPointerOverOwnWindow`, `ImmediateGuiAppHost.cs:687`); popups and secondary
   windows are shown and focused natively (`:1014`, `:1077`); pointer lock is real (`:671`); time is
   wall-clock (`:365`); fallback fonts load on an unawaited `Task.Run` (`:323`).
4. **There is no stable test identity.** `Ui.GetId(string)` hashes a leaf against the `Ui.Id` stack
   without pushing it (`Ui.cs:426`). Composite widgets scope by hashes: listed tabs use
   `ui.Id(tab.Id)` where `tab.Id` came from `GetId` (`UiTabStrip.cs:207`, `:215`), tree rows combine a
   comparer hash (`UiTreeView.cs:250`), menus use `HashCode.Combine` and `UniqueId`
   (`UiMenus.cs:14`, `:751`). `UiFrames.ClickOn("save")` only reaches root-scope widgets.
5. **`GuiMcpServer` is welded to the retained driver**: its constructor takes `GuiDriver`
   (`GuiMcpServer.cs:25`), and its tools return prose, with JSON snapshots inside text content.

## Decisions

### D1. `GpuFrames`: `UiFrames` with a real GPU canvas, no window

`GpuFrames<TState>` keeps `UiFrames`' whole input surface (window-qualified coordinates, pointer
down/up/move with modifiers, drag, key down/up, `Type`, `Compose`) and its stepping, and swaps the
recording canvas for a real `OpenGlRenderedCanvas` or `MetalRenderedCanvas` drawing into an
offscreen target (D2). Selector conveniences (D4) are built on those primitives and never replace
them: drags, splitters, sliders and custom-canvas work need raw pointer control.

**Ownership is explicit.** A `GpuDevice` is created by the test (an xUnit collection fixture; the
repo is on xUnit 2.9, which has no assembly fixtures) and **passed in**. The device owns one dedicated render thread and the
GL context or Metal device on it; every `GpuFrames` call runs synchronously on that thread.
Mutable resources — font atlas, registered fonts, images — are **per session** and disposed on the
owner thread when the session is. Shaders are compiled per session at first; pipelines are split out
of `GlSharedResources`/`MetalSharedResources` (`GlSharedResources.cs:16`,
`MetalSharedResources.cs:19`) and cached on the device only if measurement shows it matters.

**One OpenGL device per process.** The GL entry points are static fields of `GL46`, written by
`Import` (`GL46.cs:1396`, `:4200`), and the canvas calls them statically; on Windows, addresses from
`wglGetProcAddress` are only valid for contexts with the same pixel format. So a second live OpenGL
`GpuDevice` in one process is not supported, and creating one throws. That check is the one piece of
process-wide state here, it is flagged in the code, and it is total: it only guards creation.
Device-owned GL dispatch would lift it, but it means threading a dispatch object through the whole
OpenGL renderer, and no use in this plan needs two GL devices at once. Metal has no such state.

**Context per platform**, none through GLFW (its init and teardown are main-thread-only and
process-wide, `GlfwApp.cs:24`, `:217`):

- **Windows:** WGL on a hidden Win32 window created on the render thread (Win32 permits windows on
  any thread). Function loading resolves GL 1.1 entry points from `opengl32.dll` exports and the rest
  through `wglGetProcAddress`, replacing what `Import(Glfw.GetProcAddress)` does in production
  (`OpenGlApp.cs:19`). Requires a desktop GL 4.1 core context, as production does.
- **Linux:** EGL with `EGL_MESA_platform_surfaceless` and `EGL_KHR_surfaceless_context`, GL 4.1 core.
  **Initially this means Mesa** (hardware Mesa drivers and llvmpipe); other vendors' EGL is not
  promised. An unsupported configuration fails at device creation with the missing extension named.
- **macOS:** `MTLCreateSystemDefaultDevice` on the render thread; creation, every frame and teardown
  each run inside an autorelease pool. Fails loudly when there is no device (`MetalApp.cs:34`).
  **Apple Silicon only, initially.** The renderer allocates the font atlas and uploaded images as
  shared-storage textures (`MetalSharedResources.cs:213`, `MetalImageManager.cs:242`), which Apple
  does not support on Intel/AMD GPUs. That is a bug in the shipping renderer, not in testing, and is
  fixed there (managed storage with explicit synchronisation on non-Apple GPUs); `GpuDevice` refuses
  a non-Apple-family GPU with that reason until it is.

**Required versus optional GL functions.** The importer loads GL 4.6 and some entry points are
legitimately absent on a 4.1 context (`GL46.cs:4190`); the renderer already falls back when
base-instance drawing is missing (`OpenGlRenderedCanvas.cs:48`). Device creation checks the set the
4.1 rendering and offscreen paths require, as `GlDriverCheck` (`GlDriverCheck.cs:9`) does for the
desktop host, and fails naming the missing ones; later functions stay optional.

**Text capture.** The GPU canvas turns text into glyph instances (`RenderedCanvasBase.cs:578`) and
keeps no record of it, while `UiFrames`' drawn-text queries come from `RecordingCanvas.Texts`
(`UiFrames.cs:197`). `Ui` takes an `ICanvas` (`Ui.cs:25`), so `GpuFrames` wraps each real canvas —
main and every popup — in a thin `ICanvas` that records each text and glyph-run submission, with
the surface, clip and transform in effect, and delegates measuring and drawing to the real canvas.
Popup canvases are surface-local (`LayoutTree.cs:440`), so each record carries its surface and is
mapped to main-window UI space by that surface's origin.

**Lifecycle subset.** `GpuFrames` runs `OnUpdate`, real font registration and posted work. It does
not run startup with the real host (`ImmediateGuiApp.cs:42` takes the concrete host); services come
from the test. What needs real startup is a `RemoteApp` job (D6).

**Popups and `ui.Window` dialogs** each get their own offscreen target from a GPU popup host, so the
agent can look at one window or at a composite of every popup over the main window. Placement runs
against a fake monitor area.

### D2. Offscreen targets and the pixel contract

Each target owns its logical size, DPI scale and physical pixel size explicitly; draw + readback is
separate from native presentation (`GlfwWindowBase.RenderNow` always presents,
`GlfwWindowBase.cs:269`).

- **OpenGL:** an FBO with an RGBA8 colour attachment, completeness checked, read with `glReadPixels`
  under explicit read/draw bindings.
- **Metal:** a private-storage render-target `MTLTexture`, blitted into a shared-storage `MTLBuffer`
  (portable across Apple Silicon and Intel/AMD Macs), command buffer awaited and its error checked.

**Readback is one format everywhere:** RGBA, rows top-down, **premultiplied alpha** — what both
renderers' source-alpha RGB / separate-alpha blending produces over transparent black
(`OpenGlRenderedCanvas.cs:178`, `MetalSharedResources.cs:156`). Composites blend premultiplied
surfaces; PNG export un-premultiplies. Pixels are read only when a `Look` or pixel assertion asks.

### D3. Stepping and readiness: virtual frames, real deadlines

Two different kinds of waiting, kept apart:

- **Frame quiescence (virtual).** `Advance(seconds)` steps frames at a fixed frame time. `Settle(max)`
  steps until no non-ambient update is scheduled, or `max` virtual seconds pass, and reports what is
  still animating. Perpetual animations (spinner, `UiProgress.cs:20`; caret blink,
  `UiTextField.cs:351`) mark their `RequestUpdateIn` as **ambient** and never hold `Settle` open.
- **Readiness (real time).** Work that runs off the UI thread — font loading, file IO, an awaited
  task — finishes in wall time, not virtual time; draining posted work does not make it finish
  (`UiSynchronizationContext.cs:7` only queues the continuation). `WaitFor(selector, timeout)` and
  `WaitUntilReady(timeout)` yield real time and loop on the owner thread: run posted work, then step
  frames for as long as anything asked for one — posted work, `RequestFrame` (`Ui.cs:177`), or a
  `RequestUpdate` such as the deferred focus changes (`Ui.cs:624`, `:647`) — until a frame ends with
  none of these pending and no tracked background work outstanding. Virtual time does not advance
  while waiting, so scheduled animations do not race ahead of real work. Framework font loading is
  tracked background work: readiness includes pending font loads and the relayout and redraw after
  their registration. **`GpuFrames.Running` returns only once the app is ready**, so the first `Look`
  never shows fallback glyphs. Phase 1 ships `WaitUntil(predicate, timeout)`; selector waits come
  with D4.

An action returns after the frames it needs have completed and reports the frame number. A click is
move → frame → press → frame → release → frame, because hit testing uses the previous frame
(`Ui.cs:356`). Nothing blocks the render thread waiting for a frame it must run.

### D4. Identity, snapshot and safe targeting

**Test identity is a typed path, carried separately from interaction hashes**:

```
TestPath = [Segment]        Segment = Named(string) | Keyed(string)
```

- A widget given a string id records `Named(id)`.
- Composite widgets record **semantic keys**, never hashes: a listed tab records its id-or-label; a
  tree row records `Keyed(model.KeyOf(node))` through a new `ITreeModel` key projection
  (`TreeViewState.cs:45` has none today); a menu item records its label path.
- `ui.Id(int)` scopes record nothing, because the int may be a hash; a caller that means a domain
  index says so with `ui.Id(key: "row-3")`.
- Segments are typed and their text form escapes `/`, so `"a/b"` as one name and `a` then `b` never
  collide. A selector that matches more than one element throws, listing every match.

**The snapshot is built from the completed frame** — after layout, clipping and focus traversal
(`Ui.cs:390`) — so focus, values and geometry are final:

```
SemanticNode { Path?, Role, Label?, Value?, States, Surface, Bounds, Visibility, Children }
```

- **Coordinates are the main window's UI space**, which is what popup and `ui.Window` rects already
  use (`PopupHost.cs:5`, `Ui.Windows.cs:19`). `Surface` names the window a node draws on, with that
  surface's origin in main-window UI space and its DPI, so a crop knows which target and offset to
  read. UI-space and surface-pixel coordinates are distinct types at the driver boundary.
- `Visibility` is `Visible | Clipped | ScrolledOut | Hidden`. Virtualized rows not built this frame
  are absent, and a lookup for one says so.
- `Role` is a closed enum; `States` is a flags enum.

**Selector actions validate their target through the real hit test.** Before pressing, the driver
picks a point inside the element's clipped bounds and runs it through the same topmost-hit and
modal rules input uses (`Ui.cs:777`); after the move frame it checks again. A target that does not
win is reported as `Occluded(by: node)` or `Blocked(by: modal)`, not clicked.

Elements with no identity are found by role + label or by text. The snapshot is built only when
asked for.

### D5. `Look`: the artifact an agent reads

- **`Look(name)`** writes `scratch/<name>.txt` (the snapshot) and `scratch/<name>.png` at the
  frame's own DPI, and prints both paths. `scratch/` is git-ignored.
- **`Look(name, selector)`** crops the existing framebuffer to the element plus a margin, **at the
  same DPI** — a rerender at another DPI changes glyph baking, snapping and minimum stroke widths
  (`RenderedCanvasBase.cs:262`, `:281`) and can hide the very defect being inspected.
  `magnify: 4` scales the crop up with nearest-neighbour. Rendering at another DPI is a separate,
  explicit call, and its file name carries the DPI and frame number.
- **Failures are self-explaining:** a failed lookup, occluded target or timeout writes a `Look` of
  that moment and puts its paths in the exception message.
- **`ZGF.Gui.Immediate.Scratch`** is a test project that already references the testing packages,
  the sample apps and a `GpuDevice` fixture, so an agent adds one file and runs one command.

### D6. `RemoteApp`: the out-of-process host

For real startup, real OS windows for popups, and the shipped binary.

**Automation mode** is entered only by an explicit launch flag (`--zgf-automation <port> <token>`).
In it, `ImmediateGuiAppHost` substitutes every platform effect:

- **Input** only from the automation queue; pointer-over and foreground come from automation state.
- **Windows** are created hidden, never shown or activated, and render to offscreen targets (D2).
  Show, front, maximize and pointer-lock requests are recorded.
- **Logical keyboard focus** (`Ui.Focus`, `Ui.cs:598`) works as normal.
- **Time** is virtual, with the readiness rules of D3.
- **OS services** — file pickers, clipboard, system menu bar, open file/URL, reveal — are fakes
  scripted through the protocol: queue picker answers, set and read the clipboard, read recorded
  effects and invoke menu items. An unanswered picker cancels, as `TestFilePicker` does
  (`TestDoubles.cs:30`).

The acceptance flows are: open a file via a queued picker answer and look at the result; drag a
splitter; open a popup and a dialog and assert that **no** native window was activated.

**Wire contract.** Commands and results are typed records defined independently of MCP: the raw
pointer and key primitives of D1, the selector conveniences of D4, stepping and readiness from D3,
the fake-service commands above, `Snapshot` and `Look`. Results are a sum type — `Ok(frame,
payload) | NotFound | Ambiguous | Occluded | Blocked | Timeout | Unsupported | Rejected` — carried as
MCP structured results with output schemas and parsed back into records by the client, which checks
protocol version and capabilities on connect.

**Transport seam.** `GuiMcpServer` takes tool sources only; the retained `gui_*` tools become a
`RetainedGuiTools : IMcpToolSource` over `GuiDriver`; immediate automation is a separate tool source
in a desktop-side assembly. `ZGF.Gui.Immediate` gains no dependency on the retained host.

**Live mode** — the same tool source started from a debug option on a normally running app, as the
retained `GuiApp` does (`GuiApp.cs:362`) — is **inspection-only**: `Snapshot` and `Look`, nothing
that injects input. On a normal host an injected click can open a dialog, which is shown and focused
natively (`ImmediateGuiAppHost.cs:1014`), and closing it focuses the main window (`:1038`), so any
input in live mode would break the no-takeover guarantee. Everything else returns `Unsupported`.
Driving the UI is `GpuFrames` or `RemoteApp`.

### D7. Retire `RasterCanvas` after its replacements exist — off the critical path

| Test | What it really checks | Moves to |
|---|---|---|
| `CaretOffsetsTests` | allocation-free measure and draw through the production canvas | `CaptureCanvas` (`ZGF.Gui.Tests/CaptureCanvas.cs`), promoted to `ZGF.Gui.Testing` |
| `FontRegistryTests` | glyph-atlas reclamation; needs a `RenderedCanvasBase` | `CaptureCanvas` |
| `UnstyledTextScaleTests` | real-font layout | `CaptureCanvas` |
| `PixelSnapTests`, `RoundedClipTests`, `RasterImageTests`, `RasterFramesTests` | pixel coverage | `GpuFrames` pixel assertions |
| `HarnessDebugTests` (retained `SaveScreenshot`, `HarnessDebugTests.cs:107`) | retained screenshots | `GuiTestHarness` drawing its root onto a D2 offscreen target |

So the retained harness gets a GPU screenshot path too (`GuiTestHarness.cs:216` draws its root onto
whatever canvas it is given). `PixelGridAudit` inspects layout, not pixels, and stays. Until
deletion, the raster APIs' docs say "approximate, not what ships".

### D8. Later

- **Native platform input** (`[NativeInput]`): GLFW event translation, the native IME bridge and OS
  key mapping need a visible window and real OS input. An opt-in suite run deliberately by a person;
  the only user of `OsKeyboard`/`SendInput`.
- **`Compare(before, after)`**: a highlighted diff image and the list of snapshot nodes that moved,
  appeared or changed state.
- **Backend conformance:** the same scenes on OpenGL and Metal (a Mac has both) compared under a
  tolerance, with analytic assertions; no golden images, since drivers differ between machines.
- **`IUiDriver`**, extracted from what `GpuFrames` and `RemoteApp` actually share once both exist.

### D9. Agent-facing guidance

`docs/testing.md`, linked from `CLAUDE.md`, shipped **with phase 1** and extended per phase:

| You want to check | Use |
|---|---|
| Widget logic, layout, input handling | `UiFrames` (in-process, parallel, ms per test) |
| Production staging, fonts, allocations | `CaptureCanvas` |
| What it looks like; a flow through the UI | `GpuFrames` + `Look` in `ZGF.Gui.Immediate.Scratch` |
| Startup, real OS windows, the shipped binary | `RemoteApp` |
| An app the user already has open | MCP `gui_*` tools (live mode) |

## Phasing

1. **The loop, end to end.** `GpuDevice` and `GpuFrames` (D1, D2) on Windows, Apple Silicon macOS
   and Linux/Mesa, using `UiFrames`' existing input and string ids; the text-capturing canvas wrapper;
   `Look(name)` with captured text and rects and a PNG; the scratch project; ambient updates,
   readiness and `WaitUntil(predicate)` (D3); `docs/testing.md`. Measured as the whole invocation an agent pays — build, process start, device
   creation, first frame, `Look` — cold and warm. Tests cover repeated device creation and disposal
   and a translucent popup shadow over a coloured background.
2. **Identity, snapshot and safe targeting** (D4), with the stock widgets annotated and
   `Look(name, selector)` crops. Paths tested across processes, reordered rows and duplicate labels.
3. **`RemoteApp`** (D6): transport seam, automation mode, typed contract, fake services, the three
   acceptance flows; then live mode.
4. **Retire `RasterCanvas`** (D7).
5. **D8**, as needed.

## Risks and open questions

- **An agent sees its own machine's backend.** Until conformance (D8) exists, a screenshot on
  Windows (OpenGL) may differ from what a Mac user sees (Metal) in renderer details; layout and text
  are shared.
- **Linux beyond Mesa** is not promised in phase 1.
- **Intel/AMD Macs** wait on the renderer's texture-storage fix (D1).
- **One OpenGL device per process** (D1). Tests needing two at once would need device-owned dispatch.
- **Windows WGL without GLFW** duplicates a little context and loader code GLFW does today; it lives
  with `GpuDevice`, not in the shipping host.
- **Semantic coverage:** custom drawing is invisible to selectors until annotated; drawn text with
  rects is the fallback, and raw coordinates always work.
- **Web views (CEF)** are out of scope for the first pass in both hosts.
