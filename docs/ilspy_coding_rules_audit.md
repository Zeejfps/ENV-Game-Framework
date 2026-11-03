# ILSpy coding rules audit

*2026-10-28, rechecked against the code 2026-11-03. ZGF.ILSpy (197 files, 28.9k lines) and ZGF.ILSpy.Core (99 files, 16.8k lines) against [coding_rules.md](coding_rules.md).*

These are reviewer findings with file and line, not re-checked one by one. Not deeply covered: byte parsing in `ResourceImage`, `ResourceText`, `BundleContents`, and `ZGF.ILSpy.Core/Diagrams/*`.

## Rule 1: illegal states representable

### Escape hatches

- Force-unwrapped fields valid only in some states: `MetadataGridState.Grid!` and `MetadataGridView`'s `_state.Columns!`, `ComparisonPicker.Compared!`, `Search._query!`, `ThemeFolderWatch._pendingLoss!`, `NuGetPickerView`'s `icon!`, `BrowseTree.Reveal(View.Selected!)`, `TreeViewState.Selected` meaningful only while `HasSelection`.

## Rule 2: coupling

- `AppState` is a god object: referenced from 50 files, 5 mutable public fields. Its constructor does file I/O through functions that take `AppState` back (`AppState.cs:192-197`). `UiState` is referenced from 55 files.
- Views take the whole `AppState` even when they use one member: `ErrorDialogView` (`.Errors`), `ExportStatusView` (`.Export`), `AssemblyLists*DialogView` (`.Lists`), `BookmarkImportDialogView` (`.Imports`), `AnalyzerPanelView` (`.Analyzer`). In effect a service locator.
- Core cycles: `LoadedModule` ↔ `AssemblyList` ↔ `AssemblyResolver`; `LoadedModule` → `AssemblyExport` → `LoadedModule`.
- `AssemblyList` is the Core hub: search, tooltips, docs, metadata rows and analyzer type systems all route through it.
- Static mutable caches: `ConditionalWeakTable`s in `NodeText.cs:16`, `DisplayOptionsPanelView.cs:33`, `SearchPanelView.cs:25`; `MetadataGrid.Patterns`; `FlagsSchemaInferer.cache`. Harmless memoization. The exception is the unsynchronized `_headersFor`/`_headers` in `BookmarksPanelView.cs:22`.
- `AssemblyList.Changed` is an ambient `event Action`.

## Rule 3: ordering and concurrency

- Consume-once flags depend on the view being drawn exactly once, in the right frame: `CodeViewState.FocusFind`, `ListsDialogMode.Renaming.TakeFocus`, `CrumbMenu.TakeFocus`, `UiState.IsSearchFocusPending`, `CodeFonts.TakeListingRequest`.
- Cross-frame layout: `CodeView.cs:292-320` hit-tests with the previous frame's rect; `Position`/`TopAnchor` do nothing until the first draw sets `RowHeight`; `BookmarksPanelView.cs:62, 67` call `Selection.TakeFrom` before and after the rows, an order nothing enforces.
