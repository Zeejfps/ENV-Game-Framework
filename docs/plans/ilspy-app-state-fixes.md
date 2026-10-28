# ILSpy app-state fixes

What is left to fix in how `ZGF.ILSpy` models what is open over its screen and keeps its app state
(review of 28 Oct 2026, each claim checked against the code). Each item breaks a rule in
[coding_rules.md](../coding_rules.md), mostly "make illegal states unrepresentable". Zultree
(`G:\Dev\zultree`) runs on the same app skeleton and already does most of these, so its files are
named as working examples. Ordered by priority.

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

2. **`AppState` lives in the app project and has public mutable fields.** `Theme`, `IconTheme`,
   `ApiVisibility`, `PreviewTabs`, `Locale`, `ViewSettingsChanges` and `TabPanels` (`AppState.cs:53-93`)
   can be set from anywhere, and nothing stops `AppState` from referencing the GUI. Fix: make them
   properties with setters only where a feature needs one, and over time move what does not need the
   screen into `ZGF.ILSpy.Core`, which cannot reference `ZGF.Gui.*`. Zultree keeps `AppState` in
   `Zultree.Core` for this reason.
