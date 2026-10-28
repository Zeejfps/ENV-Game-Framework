# ILSpy app-state fixes

What is left to fix in how `ZGF.ILSpy` models what is open over its screen, saves its settings, runs
background work and keeps its app state (review of 28 Oct 2026, each claim checked against the code).
Each item breaks a rule in [coding_rules.md](../coding_rules.md), mostly "make illegal states
unrepresentable". Zultree (`G:\Dev\zultree`) runs on the same app skeleton and already does most of
these, so its files are named as working examples. Ordered by priority.

## Structure

1. **What is open over the window is spread over independent fields.** `UiState` (`UiState.cs`) has
   `IsAboutOpen`, `ConfirmingLayoutReset`, `ConfirmingDisplayReset`, `MenuTarget`, `TabMenuTarget`,
   `ExportMenuTarget` (0 = none) and `SelectedExport` (0 = none), plus an `IsOpen` on `Picker`,
   `NuGetPicker`, `ComparisonPicker`, `ErrorDialog`, `ExportSetup`, `ListsDialog` and `CrumbMenu`,
   `ListsDialog.Deleting`, and `AppState.Imports.Asking`. Some dialogs stack on purpose: the delete
   confirmation over the lists dialog, the error dialog over any picker. Others never should, such as
   About over a reset confirmation, and nothing tells the two apart. Fix: model what is open as
   `Overlay` cases that carry their dialog's state, with the stacking that is meant written into them
   (a case that holds its confirmation, a list of errors that sits over the rest, as Zultree's
   `Problems` does), so that a `switch` over them breaks the build when a case is added. Zultree:
   `Overlay` / `Overlays` (`Zultree/Overlay.cs`).

2. **`Outcome<T>` is an optional-field bag.** `Background.cs:6` defines
   `record struct Outcome<T>(T? Value, Exception? Error)`, which can be built with both fields set or
   neither, and callers can read `Value` without looking at `Error`. Fix: `Ok(T) | Failed(Exception)`
   that callers must match on, keeping `BackgroundWork<T>`'s awaiter, which is better than Zultree's
   because it always continues on a later turn of the UI thread. Zultree's version
   (`Zultree.Core/Background.cs`) has the sum type but not the awaiter, so this one would suit a shared
   library both use.

3. **Saves report through side channels.** `SaveSettingsIfChanged` (`Settings.cs:14`) returns the
   seconds until a retry as a `double` and reports failure through `SaveRetry.Fail`. Fix: return
   `Saving = Unchanged | Saved | Failed(reason)` and switch on it with `UnreachableException` in the
   default branch, keeping `SaveRetry`'s timing. Zultree: `Saving` and `ShowFailedSave`
   (`Zultree/ZultreeApp.cs`).

4. **`AppState` lives in the app project and has public mutable fields.** `Theme`, `IconTheme`,
   `ApiVisibility`, `PreviewTabs`, `Locale`, `ViewSettingsChanges` and `TabPanels` (`AppState.cs:53-93`)
   can be set from anywhere, and nothing stops `AppState` from referencing the GUI. Fix: make them
   properties with setters only where a feature needs one, and over time move what does not need the
   screen into `ZGF.ILSpy.Core`, which cannot reference `ZGF.Gui.*`. Zultree keeps `AppState` in
   `Zultree.Core` for this reason.

## Adopt from the shared widgets

Recent `ZGF.Gui.Immediate.Widgets` additions that ILSpy does not use yet:

1. **Spinning buttons.** A button can spin while its work runs (`2b7eafff`). NuGet download, export
   start and Reload could use it instead of a separate `ui.Spinner` (`ExportsPanelView.cs:235`,
   `NuGetPickerView.cs:435`).
2. **Tree-row badges and spinners.** These are colored badges after a row's text, and a spinner in their
   place (`6a0a292d`, `1f732aac`). They fit assemblies still loading or resolving, or a count of
   analyzer results.
3. **Markdown and the TextArea** (`90fa1831`, `254d7096`). These could show XML doc comments in code
   tooltips as rich text, and give bookmark notes a multi-line editor.
