# ILSpy on the immediate GUI

A .NET assembly browser and decompiler built on `ZGF.Gui.Immediate`, to stress the framework with a
real app: deep virtualized trees, large code documents, split panes, tabs, menus, popups, background
work and keyboard navigation.

The decompiler engine is `ICSharpCode.Decompiler` (the engine ILSpy itself uses). Everything else —
the model the UI reads, and the UI — is ours.

## Projects

| Project | Depends on | Holds |
|---|---|---|
| `ZGF.ILSpy.Core` | `ICSharpCode.Decompiler` | Assembly list, browse tree model, decompilation to tokenized documents, reference resolution, search. No GUI. |
| `ZGF.ILSpy.Tests` | Core | xunit tests for Core, run against real assemblies (the test assembly, `System.Private.CoreLib`). |
| `ZGF.ILSpy` | Core, `ZGF.Gui.Immediate`, `ZGF.Gui.Immediate.Widgets` | The app. |

Framework gaps the app needs are filled in `ZGF.Gui.Immediate` / `ZGF.Gui.Immediate.Widgets`, with
tests in `ZGF.Gui.Immediate.Tests`, and are general-purpose — nothing ILSpy-specific there.

## Features

- Open assemblies from the command line, the native file picker (`IFilePicker`, shared with the
  retained framework), and a dialog over the running .NET runtime's assemblies. The assembly
  list, theme, language, pane sizes and window bounds persist between runs (`--settings=<path>`
  points at another settings file).
- Browse tree: assembly → references / namespaces → types → nested types and members, lazily
  expanded (large expansions in the background behind a Loading row), with kind icons/colors, and
  member signatures as labels.
- Decompile the selected node to C# or IL (segmented switch), off the UI thread, with a busy
  indicator and cancellation when the selection changes. Reload (F5) reopens every assembly in the
  background and puts the tree, the tabs and their scroll positions back.
- Code view: monospace, syntax colored per theme, line numbers, virtualized for 100k-line documents
  with an allocation-free paint path, text selection + copy, find in document, both scrollbars
  reserving their own gutters, scroll kept per tab.
- Folding: the decompiler's fold regions (type and member bodies, the using block — collapsed by
  default — and IL blocks) become `DecompiledDocument.Folds`. Chevrons in the gutter, a collapsed
  fold reads as its signature followed by a `{ … }` chip that unfolds it, Ctrl+M,M / Ctrl+M,L /
  Ctrl+Shift+[ ] and context-menu Toggle fold / Collapse all / Expand all. Copy takes folded text
  along; find, reveal and history unfold what they land in.
- Navigation: click a reference in code to go to its definition (loading the referenced assembly if
  needed), tree syncs to it; back/forward history (toolbar, Alt+Left/Right, mouse buttons 4/5)
  returns each place to the scroll position and selection it was left at.
- Tabs: open results in tabs, middle-click/close button closes; tabs and history hold `NodePath`s,
  so they survive reloads and pin no assembly.
- Search panel: type/member search across loaded assemblies, results virtualized, live as you type
  on a background thread, keyboard navigable.
- Menu bar, context menus on tree nodes (copy name, open in new tab, remove assembly), dark/light
  theme toggle, status bar with an error badge that opens a dialog of copyable error details.

## Phases

At most two agents run at once. Agents do not commit; each phase is reviewed and committed before the
next one starts.

### Phase 1 (parallel) — done

**Framework agent** — `ZGF.Gui.Immediate`, `ZGF.Gui.Immediate.Widgets`, `ZGF.Gui.Immediate.Tests`:

1. Font families: an app registers a font family from bytes before `Run`; `ui.Text`, `MeasureText`,
   `LineHeight` and `UiPainter.Text` take an optional family.
2. Pointer: secondary/middle/back/forward buttons and double-click reach `Interaction` (or a sibling
   API) so widgets can react to them.
3. Tree view: virtualized over a flattened view of an app-owned hierarchy, expansion keyed by node
   identity, single selection, keyboard (Up/Down/Left/Right/Home/End/Enter), double-click
   activation, scroll a node into view, per-row icon/color.
4. Split pane: horizontal and vertical, draggable divider, min sizes, app-owned size.
5. Tab strip: selectable, closable (button + middle click), overflow scrolls.
6. Menus: menu bar with dropdowns, context menu at the pointer, separators, shortcuts text, disabled
   items, close on Escape / outside click / pick.
7. Tooltip.
8. Native file picker: move `IFilePicker` and its platform implementations out of
   `ZGF.Gui.Desktop` to a layer both frameworks share, and expose it to immediate apps.

Tooltips, dropdowns and context menus render in native popup windows, as the retained framework's
`PopupWindowFactory` does. Code is drawn in JetBrains Mono (bundled in `ZGF.ILSpy/Assets/Fonts`),
one family per face as GitBench's `MonoFonts` does.

**Core agent** — `ZGF.ILSpy.Core`, `ZGF.ILSpy.Tests`:

1. `AssemblyList`: load/unload by path, resolve references (runtime directory, sibling files, loaded
   assemblies), thread-safe.
2. Tree model: lazily built nodes with stable identity, kind, display text, children.
3. Decompilation of any node to C# or IL into a `DecompiledDocument`: lines of text with classified
   spans (keyword, type, member, string, number, comment, IL opcode, …), spans carrying references
   and definitions, cancellable.
4. Navigation: reference → tree node (loading assemblies as needed); node → line of its definition
   in a document.
5. Search over types and members across loaded assemblies, cancellable, streaming results.

### Phase 2 (parallel) — done

**Shell agent** — `ZGF.ILSpy` except the code view: `Program`, app state, window layout (menu bar,
toolbar, split tree | tabs, search panel, status bar), open flows (args, native file picker,
runtime picker), font registration, persistence, background decompile/search wiring with `Invalidate`, history, theme.

**Code view agent** — `ZGF.ILSpy/CodeView*.cs`: a widget over `DecompiledDocument` (contract below).

```csharp
public sealed class CodeViewState { /* scroll, selection, find query, ... — owned by the widget */ }

public readonly record struct CodeViewResult(CodeReference? Navigate, bool NavigateInNewTab);

public static CodeViewResult CodeView(this Ui ui, string id, DecompiledDocument document, CodeViewState state);
public static void RevealLine(this CodeViewState state, int line);
```

### Phase 3 — done

Code review of the whole feature and fixes, run against real assemblies; the benchmark suite has
frame benchmarks for a fully expanded tree and menus, and `CodeViewFrameCostTests` measures a
100k-line document per frame (plain and with folds collapsed). Then folding, scroll owned by the
tab (`VirtualScroll` over app-owned X and Y offsets with gutters for both bars), positions in the
history, window bounds persistence and startup files posted before `Run`.
