# ILSpy coding rules audit

*2026-10-28, rechecked against the code 2026-11-03. ZGF.ILSpy (197 files, 28.9k lines) and ZGF.ILSpy.Core (99 files, 16.8k lines) against [coding_rules.md](coding_rules.md).*

These are reviewer findings with file and line, not re-checked one by one. Not deeply covered: byte parsing in `ResourceImage`, `ResourceText`, `BundleContents`, and `ZGF.ILSpy.Core/Diagrams/*`.

## Rule 2: coupling

- Views that read four or more parts of `AppState` still take all of it: `MainMenuView` (its View and Export menus), `ToolbarView`, `StatusBarView` (`DescribeActivity`), `DisplayOptionsPanelView` (the interface section), and `Shortcuts.Handle`.
- Core cycles: `LoadedModule` ↔ `AssemblyList` ↔ `AssemblyResolver`; `LoadedModule` → `AssemblyExport` → `LoadedModule`.
- `AssemblyList` is the Core hub: search, tooltips, docs, metadata rows and analyzer type systems all route through it.

## Rule 3: ordering and concurrency

- Consume-once flags depend on the view being drawn exactly once, in the right frame: `CodeViewState.FocusFind`, `ListsDialogMode.Renaming.TakeFocus`, `CrumbMenu.TakeFocus`, `UiState.IsSearchFocusPending`, `CodeFonts.TakeListingRequest`.
- Cross-frame layout: `CodeView.cs:292-320` hit-tests with the previous frame's rect; `Position`/`TopAnchor` do nothing until the first draw sets `RowHeight`; `BookmarksPanelView.cs:62, 67` call `Selection.TakeFrom` before and after the rows, an order nothing enforces.
