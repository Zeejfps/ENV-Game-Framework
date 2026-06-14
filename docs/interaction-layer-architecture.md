# Interaction layer architecture: reusing component views across input modalities

Status: **design / proposed** (not yet implemented)
Scope: how ZGF GUI components (text input, scroll bars, …) are structured so one
component **view** can be driven by different **controllers** per platform and
input modality (keyboard/mouse on desktop & web, touch/gestures on mobile).
Relates to: `docs/web-font-rendering.md` (the web port that motivated this).

---

## 1. The question

We want to reuse a component — say the text input — across platforms whose
*input modality* differs:

- desktop & web: keyboard + mouse,
- mobile: touch + gestures (no hover, no physical keyboard focus model).

The **rendering** (the view: text, caret, selection, scrolling) is identical
everywhere. Only the **input handling** (which gesture/key maps to which edit
operation) changes. So the view must be reusable while the controller varies.

The tension surfaced while planning the web port: the interaction layer
(InputSystem, controllers, components) currently lives in `ZGF.Gui.Desktop`, and
the naïve reading was "controllers are platform-specific, so this can't be
shared." That reading is wrong — see below.

---

## 2. Key finding: the split already exists (for the right components)

Two facts from the current code decide the whole design:

**(a) The interaction stack is platform-neutral, not desktop-bound.**
`ZGF.Gui.Desktop/Input/` (the `InputSystem` and all event types) references only
`ZGF.Geometry`. The controllers (`KeyboardMouseController` and subclasses)
reference only `ZGF.Gui.Desktop.Input`. **None of it touches `IWindow`, GLFW, GL,
or Metal.** It is neutral C# that merely *lives* in the desktop assembly. The only
genuinely desktop-bound piece is `DesktopInputSystem` — the adapter that pumps
OS/`IWindow` events into the neutral `InputSystem`.

**(b) The view already exposes a modality-agnostic command surface.**
`TextInputView` exposes imperative operations:

```
StartEditing / StopEditing
MoveCaretTo(PointF) / MoveCaretLeft|Right|Word / MoveCaretUp|Down / SelectAll
Enter(char) / Enter(ReadOnlySpan<char>) / Delete / DeleteWord / Clear / SetText
GetSelectedText / Text / IsEditing / IsSelecting
```

`TextInputViewKbmController` is thin: it maps keyboard/mouse events onto these
calls. A `TextInputTouchController` would map tap / long-press / drag-to-select
onto the **same** calls. The view does not change.

And crucially, **`TextInputView` does not wire its own controller** — the
controller is attached externally (via `InputSystem.RegisterController` /
`ControllerBehavior`). So the text input is *already* the target design: neutral
view + externally-supplied, swappable controller.

> Implication: controllers are **per-modality, not per-platform**, and they are
> neutral. Forking them "per platform" would duplicate neutral code; bundling the
> view with one controller would duplicate the *view* per modality. Neither is
> wanted.

---

## 3. The wrinkle: four self-wiring components

Most component views are clean like `TextInputView`, but four **construct and
attach their own keyboard/mouse controller** in their constructor via
`UseController(...)`:

- `CalendarView`
- `HorizontalScrollBarView`
- `VerticalScrollBarView`
- `VirtualRowListView`

(e.g. `HorizontalScrollBarView` calls
`_thumbView.UseController(_ => new HorizontalScrollBarThumbViewController(...))`.)

This self-wiring hard-binds those views to the KBM controller, which blocks
driving them with a touch controller. To make them as modality-flexible as the
text input already is, the fix is mechanical and per-component: **remove the
self-wiring; expose the same view as a neutral command surface; let the
composition layer attach the modality-appropriate controller.**

---

## 4. Decision: layering

```
ZGF.Gui                         core: View, Context, behaviors, ICanvas, styles
ZGF.Gui.Components   (neutral)  component VIEWS (command surfaces)
                                + InputSystem + event types + ControllerBehavior
ZGF.Gui.Controllers.Kbm         keyboard/mouse controllers  (desktop + web)
ZGF.Gui.Controllers.Touch       gesture controllers         (mobile, later)
<host packages>                 only the event SOURCE/adapter + window seams:
                                  ZGF.Gui.Desktop  -> DesktopInputSystem (IWindow)
                                  ZGF.Gui.Web      -> WebInput (DOM)
                                  (future mobile)  -> a touch feeder
```

Rationale:

- **Views are neutral → `ZGF.Gui.Components`.** They render and expose commands;
  they know nothing about input devices.
- **InputSystem + events are neutral → same package.** They are the dispatch hub
  the controllers plug into; component views already depend on them.
- **Controllers are per-modality → their own package(s).** `Kbm` is shared by
  desktop and web (same modality); `Touch` is added when mobile lands. The
  `InputSystem` already accepts *any* `IKeyboardMouseController` per view, so a
  platform can override a specific component's controller without forking others.
- **Only the event source is platform-specific.** `DesktopInputSystem`
  (reads `IWindow`) and `WebInput` (reads the DOM) convert native input into the
  neutral event types and pump the neutral `InputSystem`. This is the one piece
  bound to a platform — and it is a *feeder*, not a controller.

Whether the controller package is split by modality now or later is a sizing
call; the important boundary is **view (neutral) vs controller (modality) vs
source (platform)**.

---

## 5. What stays genuinely platform-specific

- **The input source/adapter:** `DesktopInputSystem` ↔ `IWindow`,
  `WebInput` ↔ DOM, a future mobile touch feeder. (One small file each.)
- **Context-menu popups:** on desktop a context menu spawns an OS popup window
  (`IContextMenuHost` / `IWindowCoordinates`); on web it becomes an overlay or an
  in-canvas layer. This windowing seam must be re-implemented per platform
  regardless of the layering, so the two `ContextMenu/*` files that touch it are
  excluded from the first neutral move.
- **Focus/hover semantics:** keyboard focus and pointer hover are
  desktop/web concepts; touch has neither. The views already separate edit state
  (`IsEditing`/`StartEditing`) from any focus notion, so a touch controller can
  drive editing with no focus model — but controllers, not views, own this
  difference.

---

## 5a. Platform services (clipboard, file picker, drag-and-drop)

Some capabilities aren't input *events* or rendering — they're OS/browser
services the UI calls into: clipboard, file open/save, etc. These follow the same
view/controller/source split, expressed as **neutral service interfaces injected
via `Context`**, with one implementation per host. `IClipboard` (already in
`ZGF.Gui`) is the precedent; `IFilePicker` is the next one.

Two rules keep these abstractions honest across platforms:

- **Model content, not paths.** The browser sandbox returns file *content*, never
  a filesystem path. So `IFilePicker` yields a `PickedFile` exposing
  `OpenReadAsync()` (a stream) — desktop wraps an OS path behind the same surface.
  A path-typed API would be a desktop-ism that can't cross to web/mobile.
- **Respect user-activation.** A web file picker (and similar gated APIs) can only
  open synchronously inside a user gesture. So the click that opens it must be
  dispatched **synchronously** from the platform event source into the controller
  — not deferred to the next polled frame. This is a concrete reason the per-
  platform *event source* should pump button events synchronously (as
  `DesktopInputSystem` already does), and a constraint controllers that call gated
  services must honor.

Drag-and-drop of files is **input**, not a one-off service call: the source
surfaces `FileDragEnter/Over/Drop` carrying GUI-space coordinates (for hit-test +
drop-zone highlight) and, on drop, the `PickedFile` payload — routed through the
neutral `InputSystem` to the view under the pointer like any other pointer event.
(Browsers withhold file contents until `drop`, so dragover can only drive the
highlight.)

Platform implementations: web = hidden `<input type=file>` / drag events on the
canvas (`ZGF.Gui.Web/Files`); desktop = OS dialog (note: GLFW has **no** file
dialog, so a platform dialog or native call is needed) + `glfwSetDropCallback`
for drops; mobile = document picker / share sheet. A first-cut web implementation
exists in `ZGF.Gui.Web`; `IFilePicker`/`PickedFile` should graduate to the neutral
package alongside `IClipboard`.

## 6. Migration plan (incremental, low-risk)

Because the moved code is neutral, this is a **relocation**, not a rewrite — it
compiles identically; the only churn desktop sees is namespaces and a project
reference. Suggested order, each step independently shippable:

1. **Create `ZGF.Gui.Components`.** Move `Input/` (InputSystem + events) and
   `Controllers/` framework (`IKeyboardMouseController`, `ControllerBehavior`,
   `KeyboardMouseController`) into it. Re-point `ZGF.Gui.Desktop` to reference it.
   Keep `DesktopInputSystem` in `ZGF.Gui.Desktop`.
2. **Move the already-clean component views** (text input, etc.) into
   `ZGF.Gui.Components`. Their KBM controllers go to `ZGF.Gui.Controllers.Kbm`
   (or stay co-located initially if a separate package is premature).
3. **Un-self-wire the four bundlers** (Calendar, both ScrollBars, VirtualRowList):
   remove the in-ctor `UseController(...)`; have the composition layer attach the
   controller. This is the only behavioral edit; do it component-by-component with
   the desktop app as the test.
4. **Web host** references `ZGF.Gui.Components` (+ `Kbm` controllers) and attaches
   controllers, feeding the neutral `InputSystem` from `WebInput` instead of the
   current polled snapshot.
5. **(Future) `ZGF.Gui.Controllers.Touch`** for mobile, reusing every view
   unchanged.

Namespaces move from `ZGF.Gui.Desktop.Input` / `.Controllers` / `.Components.*`
to neutral names (`ZGF.Gui.Components.*`, `ZGF.Gui.Controllers.*`). This is the
bulk of the diff and is mechanical.

---

## 7. Rejected alternatives

- **Bundle view + KBM controller as `Desktop.Components`; mobile gets its own
  `Mobile.Components`.** Duplicates the neutral *view* per modality — the exact
  reuse we want to keep. Rejected.
- **Extract only the `IWindow`-dependent code; leave components in
  `ZGF.Gui.Desktop`.** Cheapest, but components stay in a desktop-named package
  with KBM controllers bundled, so no cross-modality reuse. Acceptable *only* if
  touch/mobile is permanently out of scope — which contradicts the motivation.
- **Copy the interaction layer into each host.** Zero desktop risk, but creates a
  second living copy of ~40 actively-developed interaction files that then drift.
  Fine for a throwaway spike, not for a maintained multi-platform target.

---

## 8. Relationship to the web port

This restructure is **not** on the critical path for the web port's core value:
rendering and fonts (`docs/web-font-rendering.md`) are independent and already
drafted, and the web host can render the toolkit today. This layering only gates
*interactive shared components* (driving the real `TextInputView` etc. from the
browser). It can be sequenced after the font/render spike is validated.

---

## 9. Task list

- [ ] Create `ZGF.Gui.Components`; move `Input/` + controller framework; re-point
      `ZGF.Gui.Desktop`. (Relocation, no behavior change.)
- [ ] Move clean component views into `ZGF.Gui.Components`; KBM controllers into
      `ZGF.Gui.Controllers.Kbm` (or co-locate initially).
- [ ] Un-self-wire `CalendarView`, `HorizontalScrollBarView`,
      `VerticalScrollBarView`, `VirtualRowListView`; attach controllers from the
      composition layer.
- [ ] Decide the context-menu popup strategy per platform (OS window vs overlay).
- [ ] Web host: reference the neutral packages, attach `Kbm` controllers, feed
      `InputSystem` from `WebInput`.
- [ ] (Future) `ZGF.Gui.Controllers.Touch` + a mobile touch feeder.
