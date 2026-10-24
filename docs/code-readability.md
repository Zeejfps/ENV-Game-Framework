# Code readability rules

Code should be digestible at a glance: no decoding lines, details one jump away.

## Lines and levels

- Each line does one thing. Split lines that mix a call with `??`, a ternary, or a transformation.
- Each method stays at one level of abstraction. Entry points read as a list of named steps.
- `Program.cs` holds only the top-level steps; no helper functions.
- Method names are verbs (`CreateWindowConfig`, `ListExistingAssemblies`), not nouns (`WindowConfig`, `StartupAssemblies`).
- Type names are nouns for what the type holds or is (`NavigationHistory`, `ErrorLog`, `BrowseTree`). No action names (`BookmarkImport`, `SettingsSync`) and no role suffixes like `-er`, `-Manager` or `-Model` (`IntentTracker`, `SearchModel`); name it `Search`, `Bookmarks`, `FrameworkPicker`. Feature files are the exception: they are named for the activity they cover (`Navigation`, `Bookmarking`, `Searching`).
- Extract small intent-named helpers even when only a few lines long (`ReportOpenFailure(error)`, `IsStale(ticket)`), so a caller never mixes plumbing or lookup details with intent.
- Name the intent and hide the mechanism: `settings.RememberWindow(bounds)`, not `settings.Window = AppSettings.Sanitize(bounds)`. Prefer `IlSpyApp.Launch(commandLine)` over wiring settings, app state and app together in top-level code.
- Replace tricks with intent-named calls (`node.LoadChildren()`, not `_ = node.Children`).
- Magic strings and numbers become named constants next to their use.

## State and behaviour

- App state lives in a plain data class (`AppState`): public properties and fields, its setup, and nothing the user does. It holds the app's data and which items are current (the active tab, the open list, the current history entry), and runs and is tested with no screen.
- What exists only because there is a screen lives on the app's `UiState`, one per app: widget state (each open tab's `TabView`, `TableColumns`, the trees' `TreeViewState`s), the layout (`MainLayout`), panel and dialog state (`BookmarksPanel`, `Search`, `ExportSetup`), every selection, UI flags (`IsSearchFocusPending`), and the view state the settings keep, which it hands to the app state when it changes (`KeepViewSettings`). `UiState` may read the app state; the app state never sees `UiState`.
- State with a rule that must stay true gets its own small type that guards only that rule behind private fields: stale results are dropped (`UserIntent`), the active tab is an open tab (`DocumentTabs`), a new reload cancels the old (`Reload`), settings save only when changed (`LastSave`). Never add such state as a plain field.
- Behaviour lives in feature files: static classes of extension methods on the data class, one per feature (`Navigation`, `Bookmarking`, `AssemblyLoading`). Callers read as if the methods were on the class: `w.Navigate(reference, inNewTab)`.
- Features see the app state only. They take the item they act on, not the selected one, and return what the view needs: the item made (`CreateList`), the row an item left (`BookmarkToggle.Removed`), the item gone to (`StepBookmark`).
- Views and `Shortcuts` see both. They turn the selection into an item, call the feature, then do the UI's part themselves: select, reveal, focus. Panel and dialog types hold those reactions (`BookmarksPanel.Remove`, `AssemblyListsDialog.Create`).
- A feature that runs with no view on the stack (an async continuation, or one feature called from another) and needs the view to act commands it: `w.EnqueueCommand(new ViewCommand.ShowTab(tab, beside))` queues a case of the closed `ViewCommand` record hierarchy, which `UiState` runs in order before it draws. A view calling a feature directly gets a return value instead. What the screen can read off the app state, such as the theme, the language or a cleared list, it follows each frame rather than being asked.
- A helper shared by several features is an `internal` extension in the feature that owns the concept (`ShowIn` in `Navigation`).
- Types other than the app's data class keep their behaviour on the type that owns the data (`MainLayout.OpenSearch`, `DocumentTabs.Find`).

## Ordering

- Methods follow the call chain: each method is followed by the methods it calls, in the order it calls them. A callback comes right after the method that passes it, so reading top-down follows execution.
- Shared helpers called from many places can live anywhere.

## Syntax

- Avoid uncommon syntax sugar: property patterns as null checks, collection-expression spreads, range slicing, ternaries inside arguments.
- Null checks use `x != null`, not `is { } x`. Patterns only when they do real work.
- Guard clauses with early return, one check per `if`.
- No chained ternaries. A single ternary only for trivial `a ? x : y` picks.
- Conditions with more than two parts get a name: a method (`IsCopyShortcut(stroke)`) or a local `bool`.
- Switch expressions are fine as flat lookup tables, one mapping per line.
- No increments inside expressions (`=> ++_x`); increment on its own line.
- `var` is fine everywhere. `with { }` is fine.

## Members

- Expression-bodied members (`=>`) only when the body does exactly one thing and the line is short. Otherwise use a block body, one step per line.

## LINQ

- One operator per line:
  ```csharp
  var bookmarks = found
      .Where(f => f.Place != null)
      .Select(f => Bookmark.FromRecord(f.Record, f.Place))
      .ToList();
  ```
- Lambdas inside stay small: one condition or one call.

## Lambdas

- Inline only when the body is a single expression with no nesting.
- Multi-statement bodies become named methods.
- Never nest lambdas; restructure with `async`/`await` instead.
