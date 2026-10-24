# ILSpy and immediate GUI improvements

Open issues found in `ZGF.ILSpy`, `ZGF.ILSpy.Core`, `ZGF.Gui.Immediate` and `ZGF.Gui.Immediate.Widgets`
(review of 22 Oct 2026). Ordered by priority within each section.

## Correctness

1. **Closing an assembly breaks its dependents.** `AssemblyList.Close` (`ZGF.ILSpy.Core/AssemblyList.cs:57`)
   removes it from `_known`, but dependents keep a type system over the old `MetadataFile`. Analyzing or
   navigating through them makes `TypeSystemOf` (`:199`) throw "was not loaded by this list";
   `ResolveDefinition` re-reads the file by path and can apply old tokens to a changed file.
   Fix: index loaded assemblies by `MetadataFile` and track reverse references so closing invalidates
   dependents. The same index removes the linear `FindKnown` / `TypeSystemOf` scans.
2. **References bind to the running runtime first.** `AssemblyResolver.cs:46` asks `RuntimeAssemblies.Find`
   before the target-framework resolver, so .NET Framework assemblies bind `mscorlib`, WinForms and WPF to
   .NET 10. Fix: consult `RuntimeAssemblies` only for `.NETCoreApp`, `.NETStandard` or unknown targets.
3. **Clear list doesn't stop in-flight opens.** `AssemblyLoading.OpenInBackground`
   (`ZGF.ILSpy/AssemblyLoading.cs:20`) passes `CancellationToken.None`; `ClearList` only stops `Reload`.
   Files still opening are re-listed after clearing. Fix: a list generation bumped by `Clear` that `TryList`
   checks, and opens owned by a cancellable type like `Reload`.
4. **Reload doesn't own the tabs it releases.** `Refresh` (`AssemblyLoading.cs:62-75`) releases every tab
   first; if the reload is stopped or fails with anything but `AssemblyLoadException`, tabs stay on
   "// Reloading…". A second F5 reads positions from released tabs (`DocumentTab.cs:65`) and loses scroll.
   Fix: `Reload` holds the pending tabs, carries them across restarts and settles them on stop or failure.
5. **Widget ids collide silently.** `Ui.GetId` (`ZGF.Gui.Immediate/Ui.cs:290`) is `HashCode.Combine(parent,
   label)` with no duplicate check; `LayoutTree.cs:327` overwrites rects. `GetState` (`Ui.cs:339`) replaces
   state of a different type, so two same-id widgets wipe each other every frame. Id `0` means "none" yet
   `IsHovered(0)` is true over empty space. Tree rows use only `Comparer.GetHashCode(node)`
   (`UiTreeView.cs:160`). Fix: debug duplicate-id detection in `Emit`, state keyed by (id, type), and a
   `GetId` that never returns 0.
6. **Analysis cancellation doesn't reach the scan.** The type and method-body loops in `AnalyzerScope.cs:48-84`
   never check the token. `TypeUsedByAnalyzer.cs:16-25` is PLINQ without `WithCancellation` and wraps OCE in
   `AggregateException`, which `AnalyzerTree`'s filter misses. `DerivedTypesNode.Find` (`Nodes.cs:258-274`)
   uses `CancellationToken.None` and caches forever, so later-opened assemblies never show.
   Fix: thread `ct` into inner loops, add `.WithCancellation(ct)`, compute derived types through a
   cancellable call.
7. **Settings save unevenly.** `SavedSources` (`LastSave.cs`) leaves out the folded option groups, the layout and
   the column widths, so they save only at shutdown or with another change. Fix: give each a change count that
   `SavedSources` holds.
8. **`TreeViewState._expanded` never forgets removed nodes** (`ZGF.Gui.Immediate.Widgets/TreeViewState.cs:30`).
   Closed assemblies and finished analyses stay alive all session. Fix: `Sync` drops entries whose root is no
   longer in `roots`, or a `Forget(subtree)`.
9. **Scrollbar thumbs steal focus from their list.** `Ui.cs:276` drops focus on any unclaimed left press;
   thumbs are separate hit regions (`Ui.Layout.cs:291-292, 474, 483`). Fix: an internal `KeepFocus()` for
   chrome presses.
10. **Key repeat re-activates controls.** `TakeKey(Enter/Space)` (`UiWidgets.cs:278-284`, `UiMenus.cs:520-527`)
    accepts repeats and any modifiers. Fix: `UiInput.TakePress(key)` ignoring repeats and command modifiers.
11. **Search failures are swallowed.** `AssemblySearch.Scan` (`AssemblySearch.cs:49-59`) and `WarmUp`
    (`LoadedAssembly.cs:115-124`) catch everything. Fix: catch only unreadable-metadata exceptions and stream
    others as error entries.
12. **Hidden-member filter ignores user options.** `Members.IsHidden` (`Nodes.cs:303-309`) uses default
    `DecompilerSettings`, so with YieldReturn/AsyncAwait off, generated types appear in code but not in the
    tree. Fix: derive hiding from the current settings, or rebuild roots when those toggles change.
13. **State lifetime is counted in frames** (`Ui.cs:18-19, 642-661`). Frames only render on events, so unused
    state lives forever while idle. Fix: age by `RealTime`.

## Performance

1. **Search inserts are quadratic.** `Search.Insert` (`ZGF.ILSpy/Search.cs:189-202`) inserts each row into a
   `List`; every comparison builds `LoadedAssembly.NameAndVersion` strings twice (`LoadedAssembly.cs:89`), and
   again per visible row per frame (`SearchPanelView.cs:248`). Fix: sort and merge batches, compare lazily,
   cache `NameAndVersion`.
2. **Search allocates per member.** `AssemblySearch.cs:68-110, 315-319` rebuilds `TypeName` recursively and
   concatenates `type.member` per member; each hit runs `FindNode`. Fix: cache qualified names per handle,
   compare through `MetadataStringComparer`, resolve hit nodes lazily.
3. **Lookup dictionaries never trim.** `_textStyles` (`Ui.cs:33, 559-581`), `_lineHeights`, `_cells` grow
   with animated colours or zoomed sizes. Fix: age them like `FrameCache`.
4. **Small per-frame costs.** `UiInput.cs:144` allocates a string per typed character; caret positions are
   measured uncached every frame (`UiTextField.cs:160-161`, `UiSelectableText.cs:219, 229-230`).

## Structure

1. **Widget results are inconsistent.** `TextField` returns "submitted", not "changed" (`UiTextField.cs:57-65`),
   so `CodeView.cs:718-719` compares strings and `SearchPanelView.cs:136-142` re-derives `GetId("search")`.
   `SelectableList` takes only `int` ids (`UiList.cs:223`), `TreeView` only `string` (`UiTreeView.cs:36`).
   Fix: both id overloads everywhere and a small result struct (`Id`, `Changed`, `Submitted`).
2. **Load-and-resolve logic is duplicated** in `AssemblyList.FindNode` / `Describe` (`AssemblyList.cs:92-170`),
   `Analysis.Find` (`Analysis.cs:111-122`), `ILSpyTree.Load` / `FindEntity` / `Resolve` (`ILSpyTree.cs:85-142`)
   and `AssemblySearch.Find`, each with different exception lists. Fix: one owner such as
   `LoadedAssembly.TryResolve(token)` with a single failure policy.
3. **Background-resolve-with-ticket is copied three times** (`Navigation.cs:224-236`, `:295-313`,
   `Bookmarking.cs:51-62`); `Analyzing.Analyze` (`Analyzing.cs:22-32`) skips the ticket, so late results still
   pop the analyzer. Fix: one internal `w.ResolveInBackground(...)` in `Navigation`.
4. **`RichTextLayout` duplicates TextWrapper's line breaking** (`RichTextLayout.cs:28-31, 126-225`).
   `MeasureWidestChunk` (`:72`) is unused. Fix: one break routine with single-style text as one run.
5. **`CodeView()` is a ~210-line method** (`CodeView.cs:217-427`) with ~35 mutable fields in `CodeViewState`
   (`:38-76`). Fix: split like `MetadataGridFrame` into metrics, hit-test result and press / find / menu state.
6. **`Ui` holds 68 private fields.** Fix: extract `Focus` (`_focused`, `_focusSeen`, `_focusClaimed`,
   `_focusVisible`, `_tabStops`, `_deferredTab`) and `PressTracker` (`Ui.cs:49-65`).
7. **Mutable `TextStyle` leaks.** `Ui.StyleText` (`Ui.Layout.cs:213`) returns the shared interned style with
   public fields; editing it repaints everything and breaks reference-keyed caches. Fix: an opaque immutable
   `UiTextStyle` handle.
8. **`RichText` caches layouts by list identity.** `Ui.LayOutRuns` (`Ui.Text.cs:64-75`) keys on
   `ReferenceEquals` of the run list (`:194-199`); "pass the same list while the text is the same" lives only in
   doc comments (`Ui.Layout.cs:225`). A list mutated in place gets a stale layout; a new list each frame
   re-lays out and allocates. Its only caller, `CodeTooltipView`, keeps its lists per tooltip. Fix: an
   immutable `RichTextBlock` that owns its runs and caches per width.
9. **Tree models carry view styling.** `TreePaneView.cs:139-140` and `AnalyzerPanelView.cs:17` push
   `SetStyle` every frame; `ShowNonPublic` lives in both `Workspace` and `BrowseTree`. Fix: a per-frame
   adapter in the view and one owner of `ShowNonPublic`.
10. **Analyzer headers are matched as strings across Core and the app.** `AnalyzerText.Header`
    (`AnalyzerTree.cs:322`) switches on Core's English headers (`Analysis.cs:78+`). Fix: an enum per analysis
    kind mapped to `Strings`.
11. **Shortcut labels and matching are separate** (`Shortcuts.cs:130-136` vs `:139-205`); Ctrl+W and Ctrl+Tab
    have no constant. Fix: a `Shortcut` type with `Label` and `IsPressed(ui)`, one bindings table.
12. **Types report their own errors.** `DocumentTab`, `Search` and `FrameworkPicker` each take `Func<Strings>`
    and `Action<string,string>`. Fix: return failures to the owning feature file.
13. **`MetadataGrid` contradicts its node's thread-safety promise** (`MetadataGrid.cs:57`) and its static
    regex cache (`:61`) is unbounded.

## Readability

- `ZGF.ILSpy`: `DocumentTab.cs` (chained ternaries `:65,68,71`, patterns `:119,152-162`, sugar in arguments
  `:175-194`), `Bookmarks.cs` (`:276-284, 366, 381, 437`), `BrowseTree.cs` (`:221-227, 269, 306-353`),
  `NodeText.cs:23-36`, `DocumentsView.cs:374-404`, `AppSettings.cs:355-408`; 39 `is {` patterns in 12 files.
- `ZGF.ILSpy.Core`: ~85 `is { }` checks and ~50 range slices; chained ternaries in `DocumentBuilder.cs:198-206`,
  `Analysis.cs:165-169`, `EntityText.cs:41-46`, `DecompilerOptions.cs:96`, `LoadedAssembly.cs:134-136`;
  `_ = parent.Children` in `LoadedAssembly.cs:138` and `Nodes.cs:28` instead of `LoadChildren()`;
  `Metadata/FlagsSchema*.cs`, `FilterState.cs`, `FilterStatePresenter.cs` use another style and role suffixes.
- `ZGF.Gui.Immediate*`: chained ternaries (`UiWidgets.cs:149, 157, 292`, `Ui.Layout.cs:381, 503`, `Ui.cs:497`,
  `UiMenus.cs:117`, `UiTabStrip.cs:88, 91`); property-pattern null checks (`UiWidgets.cs:66, 107`,
  `LayoutTree.cs:200, 285`, `ImmediateHost.cs:228`, `Ui.Layout.cs:198`, `UiDockSpace.cs:580`);
  `is var x and not 0` (`Ui.Layout.cs:520-531`, `UiMenus.cs:263, 550`); multi-statement lambdas
  (`UiWidgets.cs:235`, `UiSplitPane.cs:60`, `ImmediateHost.cs:147`); ~120-line `TreeView`
  (`UiTreeView.cs:36-158`) and `Field` (`UiTextField.cs:93-214`).

## Test gaps

- Analysis: Overrides, Implements, Implemented/Overridden By, Instantiated By, Exposed By, Extension Methods,
  Applied To, Uses, Assigned / Address Taken, `InternalsVisibleTo` scope, cancelling a running analysis.
- AssemblyList: close with a dependent still open, .NET Framework / netstandard binding, picking among several
  known versions, `.netmodule` resolution.
- Search reports a failing scan; tree hiding under non-default options; Derived Types after a later open.
- App: restoring last-session analyses, Ctrl+W / Ctrl+Tab, clearing during open or reload, saving on change,
  stale tooltips after assembly changes, overlapping imports, double F5 keeping scroll.
- GUI: allocation guards for TreeView, TextField, menus, tooltips, DockSpace, TableHeader; state pruning;
  `MoveFocus`; DPI change in `UiFrames`; duplicate ids; focus while dragging a scrollbar thumb.

## Docs

- `docs/plans/ilspy.md` misses the analyzer, bookmarks, metadata explorer, docking, floating windows, options
  panel, localization and XML-doc tooltips, and still shows the old four-argument `CodeView` contract.
