# ILSpy coding rules audit

*2026-10-28, rechecked against the code 2026-11-03. ZGF.ILSpy (197 files, 28.9k lines) and ZGF.ILSpy.Core (99 files, 16.8k lines) against [coding_rules.md](coding_rules.md).*

These are reviewer findings with file and line, not re-checked one by one. Not deeply covered: byte parsing in `ResourceImage`, `ResourceText`, `BundleContents`, and `ZGF.ILSpy.Core/Diagrams/*`.

## Rule 1: illegal states representable

### Escape hatches

- Swallowed: swallow-all at `LoadedAssembly.cs:258` (`WarmUp`); unfiltered catches at `BrowseSession.cs:258`, `NodeDecompiler.cs:43`, `Batches.cs:26`, `Search.cs:179`, `AnalyzerTree.cs:88`; catch-all-but-cancellation at `AssemblySearch.cs:62`, `AssemblyExport.cs:296`, `MethodBodyWithCSharp.cs:65`, `NodeContentLoader.cs:89`.
- Downcasts: analyzers cast `ISymbol` input, e.g. `(ITypeDefinition)analyzedSymbol` in `Analysis/*Analyzer.cs`; `(int)sort - 1` and `(SearchSort)(column + 1)` in `SearchPanelView.cs:193, 201` tie enum order to column order.
- `(LanguageVersion)options.LanguageVersion` in `ZGF.ILSpy.Core/Settings.cs:14` casts numerically between two mirrored enums.
- "Enable the button, then force-unwrap": `TreePaneView.cs:49-58` is correct only while disabled widgets never fire.

## Rule 2: coupling

- `AppState` is a god object: referenced from 50 files, 5 mutable public fields. Its constructor does file I/O through functions that take `AppState` back (`AppState.cs:192-197`). `UiState` is referenced from 55 files.
- Views take the whole `AppState` even when they use one member: `ErrorDialogView` (`.Errors`), `ExportStatusView` (`.Export`), `AssemblyLists*DialogView` (`.Lists`), `BookmarkImportDialogView` (`.Imports`), `AnalyzerPanelView` (`.Analyzer`). In effect a service locator.
- Core cycles: `LoadedModule` ↔ `AssemblyList` ↔ `AssemblyResolver`; `LoadedModule` → `AssemblyExport` → `LoadedModule`.
- `AssemblyList` is the Core hub: search, tooltips, docs, metadata rows and analyzer type systems all route through it.
- Static mutable caches: `ConditionalWeakTable`s in `NodeText.cs:16`, `DisplayOptionsPanelView.cs:33`, `SearchPanelView.cs:25`; `MetadataGrid.Patterns`; `FlagsSchemaInferer.cache`. Harmless memoization. The exception is the unsynchronized `_headersFor`/`_headers` in `BookmarksPanelView.cs:22`.
- `AssemblyList.Changed` is an ambient `event Action`.

## Rule 3: ordering and concurrency

- Settings have two sources of truth. View settings are written straight into `AppSettings` (`Settings.cs:109-124`); app settings live on `AppState` and are copied only at save (`Settings.cs:75-79`). `SaveDocuments` (`SavedDocuments.cs:107`) reads `Settings.Layout`, so it is correct only because `KeepViewSettings` runs first (`IlSpyApp.Frame.cs:102`, `IlSpyApp.cs:249`).
- Views mutate state two ways: directly mid-frame (`ExportsPanelView.cs:82, 259`, `TreePaneView.cs:60`, `BookmarksPanelView.cs:310`) and through `UiState.AppCommands`. Views drawn later in a frame see different state.
- The async model works by convention: 36 `async void` methods in 16 non-view files; late results are discarded five different ways (UserIntent tickets, `Assemblies.Generation`, CTS identity in `Search._runs`, ExportQueue run ids, Reload tickets). An exception after an `await` outside `Outcome` crashes the process (e.g. `complete` in `Exports.cs:121`).
- Consume-once flags depend on the view being drawn exactly once, in the right frame: `CodeViewState.FocusFind`, `ListsDialogMode.Renaming.TakeFocus`, `CrumbMenu.TakeFocus`, `UiState.IsSearchFocusPending`, `CodeFonts.TakeListingRequest`.
- Cross-frame layout: `CodeView.cs:292-320` hit-tests with the previous frame's rect; `Position`/`TopAnchor` do nothing until the first draw sets `RowHeight`; `BookmarksPanelView.cs:62, 67` call `Selection.TakeFrom` before and after the rows, an order nothing enforces.
- `ExportsPanelView.cs:105-133` snapshots runs once per frame, so Delete then an arrow key in the same frame moves against a stale list.
- `AssemblyList.UseLoadOptions` (`:175-184`) checks and sets two volatile fields without the lock.
