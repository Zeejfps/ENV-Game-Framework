# ILSpy coding rules audit

*2026-10-28. ZGF.ILSpy (197 files, 28.9k lines) and ZGF.ILSpy.Core (99 files, 16.8k lines) against [coding_rules.md](coding_rules.md).*

These are reviewer findings with file and line, not re-checked one by one. Not deeply covered: byte parsing in `ResourceImage`, `ResourceText`, `BundleContents`, and `ZGF.ILSpy.Core/Diagrams/*`.

## Rule 1: illegal states representable

Bags of optional fields are the root cause of most of the ~148 null-forgiving `!`.

### Optional-field bags that should be sum types

- **Assemblies and nodes**
    - `BrowseNode` (`BrowseNode.cs:96-160`) has 7 nullable fields marked "null for other nodes", and a 50-value `NodeKind` set independently of the subclass. Planned in [ilspy/browse_tree_architecture.md](ilspy/browse_tree_architecture.md).
- **References and documents**
    - `CodeReference` (`CodeReference.cs:31`): entity and local references share one class, and `Kind` is not tied to `FilePath`.
    - `DecompiledDocument` marks failure with `isError: bool` (`:106`, `:127`); the exception is flattened to text in `NodeDecompiler.cs:41`.
- **App state**
    - `ExportQueue.cs:254-266` spreads idle-or-running over `_cancel?`, `Running?`, `IsCancelling`, `Progress?`, forcing `_cancel!` at `:347`. `ExportStart.IsWithdrawn` is a flag every caller must check.
    - `IconValue` and `IconFont` (`IconThemeFile.cs:12-19`) have public constructors that accept invalid combinations.
    - `IThemeFile` pairs `Extends?` with `ExtendsLine` and an `IsUnreadable` flag, forcing `child.Extends!` in `ThemeCatalogOf.cs`.
    - `TabPanel(Diagram?, Comparison?)` (`ViewSettings.cs`) is really one of two; `DocumentTab.Location?`/`Node?` duplicate what `TabContent` says.
- **View state**
    - `CodeViewState` (`CodeView.cs:43-76`) has about 25 mutable fields: press, find, flash.
    - `CodeViewResult` (`CodeView.cs:210`) has 9 optional intents; lines 399, 404, 442 and 450 replace the whole result and drop intents set earlier.
    - `CrumbMenu` (`CrumbMenu.cs:17-26`) keeps `IsOpen` separate from `Node?`, hence `menu.Node!` in `OmnibarView`.
    - `AssemblyListsDialog` is `IsOpen` + `Renaming?` + `NewName` + `Deleting?`; the real states are Closed | Browsing | Renaming | ConfirmingDelete.
    - `DiagramHit(Face?, Row = -1)` (`DiagramViewState.cs:9`) should be Nothing | Card | Row.
    - `(CodeTooltip? Tooltip, int TooltipPart)` uses part 0 for "none", duplicated in `CodeView` and `DiagramView`.
    - `ExportBlocker` enum is separate from its payload, so `Clash` re-reads `setup.Clash!`.
    - Each modal has its own flag (`UiState.cs:82-165`), so two can be open at once and draw order in `MainView.cs:29-39` picks the top one.

### Bare identifiers that can be swapped

- `AssemblyPicks` holds both PDB paths and target-framework ids; a picked framework is never validated.
- Code view lines, rows, columns and match indexes are all `int`, with `NoLine = -1` as a shared sentinel; with folds, row ≠ line.
- `CodeViewPosition.TopLine` is a document line in the code view but a row index in the metadata grid.
- Export runs are `int` with 0 for "none", sharing the `ui.Id` space with section ids -1/-2 (`ExportsPanelView.cs:25`).

### Escape hatches

- Swallowed: swallow-all at `AssemblySearch.cs:56`; broad catches at `BrowseNode.cs:182`, `NodeDecompiler.cs:41`, `Batches.cs:26`, `Search.cs:182`, `AnalyzerTree.cs:88`.
- Downcasts: analyzers cast `ISymbol` input, e.g. `(ITypeDefinition)analyzedSymbol` in `Analysis/*Analyzer.cs`; `(int)sort - 1` and `(SearchSort)(column + 1)` in `SearchPanelView.cs:194, 202` tie enum order to column order.
- `(LanguageVersion)options.LanguageVersion` casts numerically between two mirrored enums.
- Reflection: `DecompilerOptions.cs:45/72` reads `DecompilerSettings` properties by name.
- "Enable the button, then force-unwrap": `TreePaneView.cs:51-60`, `AssemblyListsDialogView.cs:58` are correct only while disabled widgets never fire.
- The other ~90 catches are properly filtered with `when e is IOException or …`.

### Boundaries

- Persisted decompiler `Toggles` are a raw `ImmutableSortedDictionary<string,bool>`; stale keys make `ProducesSameCode` false and cause needless re-decompiles. `DecompilerToggle`'s public constructor lets `Apply` hit `KeyNotFoundException`.
- `FeedJson`: `ReadBool` skips the kind check, `severity.GetInt32()` can throw outside the documented `InvalidDataException`, URLs stay strings, and `NuGetSource`'s public constructor bypasses `FromUrl` validation.
- `NUGET_PACKAGES` is used as given (`NuGetConfig.cs:148`), while the config value goes through `GetFullPath`.
- `--language=bogus` becomes null (`CommandLine.cs:318`) and unknown flags are dropped (`:303`), with no feedback.

## Rule 2: coupling

- `AppState` is a god object: referenced from 50 files, about 35 public members, 7 mutable public fields. Its constructor does file I/O through functions that take `AppState` back (`AppState.cs:187-191`). `UiState` is referenced from 54 files with about 45 members.
- Views take the whole `AppState` even when they use one member: `ErrorDialogView` (`.Errors`), `ExportStatusView` (`.Export`), `AssemblyLists*DialogView` (`.Lists`), `BookmarkImportDialogView` (`.Imports`), `AnalyzerPanelView` (`.Analyzer`). In effect a service locator.
- Core cycles: `LoadedAssembly` ↔ `AssemblyList` ↔ `AssemblyResolver`; `LoadedAssembly` → `AssemblyExport` → `LoadedAssembly`; `BrowseNode` → `NodeDecompiler` → every node subclass → `BrowseNode`. The `BrowseNode` base type-tests its own subclasses.
- `AssemblyList` is the Core hub: search, tooltips, docs, metadata rows, node lookup and analyzer type systems all route through it.
- Static mutable caches: `ConditionalWeakTable`s in `NodeText.cs:16`, `DisplayOptionsPanelView.cs:33`, `SearchPanelView.cs:25`; `MetadataGrid.Patterns`; `FlagsSchemaInferer.cache`. Harmless memoization. The exception is the unsynchronized `_headersFor`/`_headers` in `BookmarksPanelView.cs:22`.
- `AssemblyList.Changed` is an ambient `event Action`.

## Rule 3: ordering and concurrency

- Settings have two sources of truth. View settings are written straight into `AppSettings` (`Settings.cs:219-235`); app settings live on `AppState` and are copied only at save (`Settings.cs:180-192`). `SaveDocuments` (`SavedDocuments.cs:107`) is correct only because `KeepViewSettings` runs first (`IlSpyApp.cs:79-82`, `248-250`).
- Views mutate state two ways: directly mid-frame (`ExportsPanelView.cs:82, 259`, `TreePaneView.cs:60`, `BookmarksPanelView.cs:310`) and through `UiState.AppCommands`. Views drawn later in a frame see different state.
- The async model works by convention: 36 `async void` methods in 16 non-view files; late results are discarded five different ways (UserIntent tickets, `Assemblies.Generation`, CTS identity in `Search._runs`, ExportQueue run ids, Reload tickets). With no `SynchronizationContext`, `Background.cs:310-313` continues on the thread pool. An exception after an `await` outside `Outcome` crashes the process (e.g. `Exports.cs:121`).
- Thread-pool work is handed the UI-only `AppState` (`Navigation.cs:97`, `Assemblies.cs:27, 149, 285, 411`, `Analysis.cs:101`, `Bookmarks.cs:128`). Today it only touches `Assemblies`; it should take `AssemblyList`.
- Consume-once flags depend on the view being drawn exactly once, in the right frame: `CodeViewState.FocusFind`, `AssemblyListsDialog.TakeNameFocus`, `CrumbMenu.TakeFocus`, `UiState.IsSearchFocusPending`, `CodeFonts.TakeListingRequest`.
- Cross-frame layout: `CodeView.cs:292-320` hit-tests with the previous frame's rect; `Position`/`TopAnchor` do nothing until the first draw sets `RowHeight`; `BookmarksPanelView.cs:61, 66` call `Selection.TakeFrom` before and after the rows, an order nothing enforces.
- `ExportsPanelView.cs:105-133` snapshots runs once per frame, so Delete then an arrow key in the same frame moves against a stale list.
- `AssemblyList.UseLoadOptions` (`:166-176`) checks and sets two volatile fields without the lock.

## Follow-ups from the LoadedAssembly split

- The nodes under an assembly name their `LoadedModule` parameter `assembly` (`Nodes.cs`, `ReferenceNodes.cs`, `Resources/ResourceNodes.cs`, `Metadata/MetadataNodes.cs`). Rename it `module`, minding the locals already called `module` in some of those classes.
- `BrowseNode`'s optional fields and its `NodeKind` set apart from the subclass (Rule 1, Assemblies and nodes) are next, planned in [ilspy/browse_tree_architecture.md](ilspy/browse_tree_architecture.md).
- The Core cycles under Rule 2 now run through `LoadedModule` ↔ `AssemblyList` ↔ `AssemblyResolver`; the split did not aim at them.
