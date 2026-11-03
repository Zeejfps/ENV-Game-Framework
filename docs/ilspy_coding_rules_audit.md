# ILSpy coding rules audit

*2026-10-28. ZGF.ILSpy (197 files, 28.9k lines) and ZGF.ILSpy.Core (99 files, 16.8k lines) against [coding_rules.md](coding_rules.md).*

These are reviewer findings with file and line, not re-checked one by one. Not deeply covered: byte parsing in `ResourceImage`, `ResourceText`, `BundleContents`, and `ZGF.ILSpy.Core/Diagrams/*`.

## Rule 1: illegal states representable

### Escape hatches

- Swallowed: swallow-all at `AssemblySearch.cs:56`; broad catches at `BrowseSession.cs:258`, `NodeDecompiler.cs:43`, `Batches.cs:26`, `Search.cs:182`, `AnalyzerTree.cs:88`.
- Downcasts: analyzers cast `ISymbol` input, e.g. `(ITypeDefinition)analyzedSymbol` in `Analysis/*Analyzer.cs`; `(int)sort - 1` and `(SearchSort)(column + 1)` in `SearchPanelView.cs:194, 202` tie enum order to column order.
- `(LanguageVersion)options.LanguageVersion` casts numerically between two mirrored enums.
- Reflection: `DecompilerOptions.cs:45/72` reads `DecompilerSettings` properties by name.
- "Enable the button, then force-unwrap": `TreePaneView.cs:51-60` is correct only while disabled widgets never fire.
- The other ~90 catches are properly filtered with `when e is IOException or …`.

### Boundaries

- The settings and bookmark files are read through tolerant `*Json` shapes (`SettingsFile.cs`), keyed by `nameof(SettingsFile.X)`, but written from the model itself: `SettingsFile.Of` and `BookmarkFile.Write` serialize the records as they are, so a change to the model for its own reasons changes the file.
  - The model carries the file's shape: `[JsonIgnore]` on `NodeDocumentRecord.Location`/`View` and `BookmarkRecord.Location`; write-only `FilePath`/`Path`/`Position`/`GridPosition` on `NodeDocumentRecord` and `Path` on `BookmarkRecord`; `[JsonPolymorphic]` on `DocumentRecord`; `[JsonIgnore]` on `CardFold.IsInherited`; `[JsonPolymorphic]` and `[JsonIgnore]`s in `ZGF.Gui.Immediate.Widgets/DockLayout.cs`.
  - With no shape at all, read and written raw: `CodeViewPosition`, `GridPosition`, `TextPosition`, `ComparisonViewRecord`, `BookmarkFoldingRange`, `BookmarkRenderedLayoutSettings`.
  - Fix: each `*Json` shape is the file's both ways, with a `From(model)` beside its `Parse()`; `SettingsFile` and the bookmarks file hold only shapes (per-entry tolerant lists written through `SerializeToElement`); the model loses its JSON attributes and file-only members. Enums stay written by name; `SettingsFormatTests` pins every key and value against checked-in fixtures.
  - Commits, each building and passing alone:
    1. Dock layout: `From` on the `Dock*Json` shapes; JSON attributes out of `DockLayout.cs`; the two JSON tests in `DockLayoutTests` (`:231`, `:247`) move to the ILSpy tests or compare structure.
    2. Bookmarks: `From` on `BookmarkJson`, `BookmarkViewStateJson`, `SavedPathJson`, `SavedPathStepJson`, `LegacyPlaceStepJson`; new folding-range and layout-settings shapes; `BookmarkFileJson` written both ways; the `BookmarkFile` record and `BookmarkRecord`'s `[JsonIgnore]`/`Path` go.
    3. Assembly lists and documents: `From` on `AssemblyListJson`, `CodeAnchorJson`, `DocumentsJson`, the tab shapes (`DocumentJson.From` an exhaustive switch) and the diagram shapes; new `CodeViewPositionJson`, `GridPositionJson`, `TextPositionJson`, `ComparisonViewJson`; `DocumentRecords.cs` and `CardFold` lose their JSON attributes and file-only members.
    4. Cleanup: `SettingsFile.Of` becomes `From`; model types out of `AppSettingsJson`; `AssemblyPathJson` if unused; the name-comparison theory goes.
  - Left after: `Place` (`List<LegacyPlaceStep>`) stays on `NodeDocumentRecord` and `BookmarkRecord` only to be written back; moving it needs `NodePath.Legacy` to keep the kind and text it was read from.
- Persisted decompiler `Toggles` are a raw `ImmutableSortedDictionary<string,bool>`; stale keys make `ProducesSameCode` false and cause needless re-decompiles. `DecompilerToggle`'s public constructor lets `Apply` hit `KeyNotFoundException`.
- `FeedJson`: `ReadBool` skips the kind check, `severity.GetInt32()` can throw outside the documented `InvalidDataException`, URLs stay strings, and `NuGetSource`'s public constructor bypasses `FromUrl` validation.
- `NUGET_PACKAGES` is used as given (`NuGetConfig.cs:148`), while the config value goes through `GetFullPath`.
- `--language=bogus` becomes null (`CommandLine.cs:318`) and unknown flags are dropped (`:303`), with no feedback.

## Rule 2: coupling

- `AppState` is a god object: referenced from 50 files, about 35 public members, 7 mutable public fields. Its constructor does file I/O through functions that take `AppState` back (`AppState.cs:187-191`). `UiState` is referenced from 54 files with about 45 members.
- Views take the whole `AppState` even when they use one member: `ErrorDialogView` (`.Errors`), `ExportStatusView` (`.Export`), `AssemblyLists*DialogView` (`.Lists`), `BookmarkImportDialogView` (`.Imports`), `AnalyzerPanelView` (`.Analyzer`). In effect a service locator.
- Core cycles: `LoadedModule` ↔ `AssemblyList` ↔ `AssemblyResolver`; `LoadedModule` → `AssemblyExport` → `LoadedModule`.
- `AssemblyList` is the Core hub: search, tooltips, docs, metadata rows and analyzer type systems all route through it.
- Static mutable caches: `ConditionalWeakTable`s in `NodeText.cs:16`, `DisplayOptionsPanelView.cs:33`, `SearchPanelView.cs:25`; `MetadataGrid.Patterns`; `FlagsSchemaInferer.cache`. Harmless memoization. The exception is the unsynchronized `_headersFor`/`_headers` in `BookmarksPanelView.cs:22`.
- `AssemblyList.Changed` is an ambient `event Action`.

## Rule 3: ordering and concurrency

- Settings have two sources of truth. View settings are written straight into `AppSettings` (`Settings.cs:219-235`); app settings live on `AppState` and are copied only at save (`Settings.cs:180-192`). `SaveDocuments` (`SavedDocuments.cs:107`) is correct only because `KeepViewSettings` runs first (`IlSpyApp.cs:79-82`, `248-250`).
- Views mutate state two ways: directly mid-frame (`ExportsPanelView.cs:82, 259`, `TreePaneView.cs:60`, `BookmarksPanelView.cs:310`) and through `UiState.AppCommands`. Views drawn later in a frame see different state.
- The async model works by convention: 36 `async void` methods in 16 non-view files; late results are discarded five different ways (UserIntent tickets, `Assemblies.Generation`, CTS identity in `Search._runs`, ExportQueue run ids, Reload tickets). With no `SynchronizationContext`, `Background.cs:310-313` continues on the thread pool. An exception after an `await` outside `Outcome` crashes the process (e.g. `Exports.cs:121`).
- Thread-pool work is handed the UI-only `AppState` (`Navigation.cs:97`, `Assemblies.cs:27, 149, 285, 411`, `Analysis.cs:101`, `Bookmarks.cs:128`). Today it only touches `Assemblies`; it should take `AssemblyList`.
- Consume-once flags depend on the view being drawn exactly once, in the right frame: `CodeViewState.FocusFind`, `ListsDialogMode.Renaming.TakeFocus`, `CrumbMenu.TakeFocus`, `UiState.IsSearchFocusPending`, `CodeFonts.TakeListingRequest`.
- Cross-frame layout: `CodeView.cs:292-320` hit-tests with the previous frame's rect; `Position`/`TopAnchor` do nothing until the first draw sets `RowHeight`; `BookmarksPanelView.cs:61, 66` call `Selection.TakeFrom` before and after the rows, an order nothing enforces.
- `ExportsPanelView.cs:105-133` snapshots runs once per frame, so Delete then an arrow key in the same frame moves against a stale list.
- `AssemblyList.UseLoadOptions` (`:166-176`) checks and sets two volatile fields without the lock.
