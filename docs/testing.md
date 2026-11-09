# Testing UI

How to check a UI change: as text, as pixels from the shipping renderer, or through the real app. None
of these show a window or touch the mouse, keyboard or focus.

## Which to use

| You want to check | Use |
|---|---|
| Widget logic, layout, input handling | `UiFrames` (in-process, parallel, milliseconds a test) |
| Production canvas staging, fonts, allocations | `CaptureCanvas` |
| What it looks like; a flow through the UI | `GpuFrames` + `Look`, in `ZGF.Gui.Immediate.Scratch` |
| Real startup, real OS windows, the shipped binary | `RemoteApp` |
| A retained-mode (`ZGF.Gui`) view tree | `GuiTestHarness` (see `ZGF.Gui.Testing/README.md`) |
| An app the user already has open | its MCP server's `gui_snapshot` / `gui_screenshot` |

Never use OS-level input (`OsKeyboard`, `SendInput`) to test UI: it takes over the user's machine.

## The loop: `GpuFrames` + `Look`

Add one file to `ZGF.Gui.Immediate.Scratch` (copy `TodoExample.cs`) and run it:

```csharp
[Collection(GpuCollection.Name)]
public sealed class MyCheck(GpuDeviceFixture fixture, ITestOutputHelper output)
{
    [GpuFact]
    public void AddTodo()
    {
        using var app = GpuFrames.Running(fixture.Device, new TodoApp(new TodoState()), width: 520, height: 640,
            output: output.WriteLine);
        app.ClickOn(Selector.AtPath("draft"));
        app.Type("Write a scratch test");
        app.ClickOn(Selector.AtPath("add"));
        app.Settle();
        app.Look("todo-added");
    }
}
```

```
dotnet test ZGF.Gui.Immediate.Scratch --filter AddTodo
```

`Look(name)` writes `scratch/<name>.txt` and `scratch/<name>.png` at the repo root and prints both
paths. Read the `.txt` first; open the `.png` only when the question is visual.

- `GpuFrames` renders with the real OpenGL or Metal canvas, with real fonts, and no window: popups and
  dialogs each get their own offscreen surface, and the PNG composites them over the main window in
  the order the snapshot lists its surfaces: windows, then popups, each by Z, then the one declared later
  on top. `dpiScale` is the monitor's; an app's `ui.Scale` multiplies it as the real host does, keeping
  the window's pixels and changing its size in points from the next frame.
- `Running` returns once the app is ready (posted work done, fonts loaded) without advancing time.
  `Settle()` runs animations out; the `.txt` header says when something is still animating.
- `Look(name, selector, margin, magnify)` crops one element at the frame's own scale;
  `LookAtScale(name, dpi)` renders again at another scale. Every harness takes a plain file name, a
  finite margin of 0 or more and a magnify from 1 to 16 (`LookArguments`).
- A failed lookup, an occluded or blocked target, or a timeout writes a `Look` of that moment and puts
  its paths in the exception.

Platforms: Windows (OpenGL), Linux (EGL, Mesa — llvmpipe when there is no GPU), macOS on Apple Silicon
(Metal). One OpenGL device may be live in a process.

## Reading a snapshot

```
# frame 30: [x,y wxh] in main-window UI points, origin top-left, y down
# surface main [0,0 520x640] @1x
window [0,0 520x640]
  textfield path="draft" "What needs doing?" [16,116 330x32] value=""
  button path="add" "Add" [354,116 51x32] disabled
  list path="items" [16,439 488x185]
    listitem path="#1/row" [16,439 488x32]
      checkbox path="#1/done" "Sketch the API" [16,439 237x32]
```

What is between the quotes after `path=` is what `Selector.AtPath("...")` takes. A segment starting
with `#` is keyed: give rows a stable string key with `ui.KeyedId(key)`, or two rows share a path and a
selector matching both throws. Other selectors: `Selector.OfRole(role, label)`, `Selector.WithText(text)`.

Widgets in `ZGF.Gui.Immediate.Widgets` annotate themselves. Custom widgets call
`ui.Semantics(role, label, states, value)` on their element; until they do, their text still shows as
text nodes, and raw coordinates (`Click(x, y)`, `Drag`) always work.

## Stepping and waiting

- Input goes into the next frame; a click is move, press and release over three frames.
- Time is virtual. `Settle(max)` steps it until nothing but perpetual animation (spinners, caret blink)
  is pending.
- `WaitUntilReady(timeout)` waits in real time for posted work, frame requests and tracked
  `BackgroundWork`, without advancing virtual time.
- `WaitUntil(predicate, timeout)` and `WaitFor(selector, timeout)` wait for posted work and frame
  requests until the predicate holds or the node appears, but not for tracked `BackgroundWork`; call
  `WaitUntilReady` first when the result depends on it.
- Animation that needs time to pass calls `ui.RequestAnimationFrame()`; a state change that needs another
  frame calls `ui.RequestUpdate()`. Readiness waits for the second, `Settle` for both.

## The real app: `RemoteApp`

```csharp
using var app = RemoteApp.Launch(RemoteLaunch.OfDll(pathToAppDll), output.WriteLine);
app.QueueFilePickerAnswer(file);
app.ClickOn(Selector.AtPath("file"));
app.ClickOn(Selector.AtPath("file/menu-open"));
app.WaitFor(Selector.WithText("groceries.txt"), TimeSpan.FromSeconds(20));
app.Look("opened");
```

That clicks the sample's File menu, which is drawn in the window on Windows and Linux. On macOS the menu
bar is the system's, so it has no nodes there: list it with `SystemMenuItems()` and invoke an item with
`PickSystemMenuItem`.

`RemoteApp` launches the app with `--zgf-automation`, and drives it over its MCP server. In automation
mode the app keeps every window hidden, takes input only from the driver, runs on a virtual clock, and
answers the file picker, clipboard and menus from fakes the script controls (`QueueFilePickerAnswer`,
`SetClipboardText`, `PickSystemMenuItem`, `Effects()`). The app must serve automation at startup, as
`ZGF.Gui.Immediate.Sample/Program.cs` does with `ImmediateMcp.ServeAutomation`. On Linux it needs a
display server. See `ZGF.Gui.Automation.Tests` for examples.

## An app that is already running

An app started with an inspection server (the sample: `--zgf-inspect <port>`, via
`ImmediateMcp.ServeInspection`) answers `gui_capabilities`, `gui_snapshot` and `gui_screenshot`. Every
other tool is listed but answers `unsupported`, so looking never sends input or moves focus.
Screenshots show the main window only.
