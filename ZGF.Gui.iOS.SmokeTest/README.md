# ZGF.Gui.iOS.SmokeTest

A **compile-only** check that the platform-independent toolkit can target iOS.

This project targets `net10.0-ios` and references `ZGF.Gui` and `ZGF.Rendering.Metal`.
It contains no app — just a probe that forces the compiler to resolve those assemblies
under the iOS target. (It deliberately does **not** reference `ZGF.Core`: the desktop
windowing abstractions live behind `ZGF.Gui.Desktop`, and the toolkit no longer depends
on them.) Its purpose is to catch problems *early*, before any real iOS app exists:

- **Restore-time issues:** a transitive dependency with no iOS runtime asset shows up
  here first (e.g. a native package that ships only Linux/macOS/Win32 assets).
- **Platform-compatibility analysis (CA1416):** desktop-only API usage in the referenced
  toolkit is flagged when its source is compiled for `ios` — but only under the opt-in
  build below. See the next section for why.

## Building — and why `-p:BuildIos=true` matters

Requires the .NET iOS workload:

```sh
dotnet workload install ios
dotnet build ZGF.Gui.iOS.SmokeTest/ZGF.Gui.iOS.SmokeTest.csproj -p:BuildIos=true
```

The `-p:BuildIos=true` flag is what makes the CA1416 coverage real. The referenced
toolkit projects (`ZGF.Gui`, `ZGF.Rendering.Metal`, `ZGF.Fonts`, `ZGF.Geometry`,
`ZGF.AppUtils`, `ZGF.KeyboardModule`, `PngSharp`) are **desktop-only by default** and
expose a `net10.0-ios` target *only* when `BuildIos=true`:

```xml
<TargetFrameworks>net10.0</TargetFrameworks>
<TargetFrameworks Condition="'$(BuildIos)'=='true'">net10.0;net10.0-ios</TargetFrameworks>
```

This keeps the iOS workload off the critical path for everyday desktop work — a normal
`dotnet build` on Windows/Linux/macOS (solution or app) never builds an iOS target and
needs no workload. The trade-off for the smoke test:

- **With `-p:BuildIos=true`** — dependencies compile their `net10.0-ios` target from
  source, so CA1416 analyzes that source and flags any desktop-only API. This is the
  build you want for an actual iOS-compatibility check (CI, or a Mac dev).
- **Without the flag** — the smoke test resolves the dependencies' desktop TFM instead.
  It still compiles (and still surfaces restore-time native-asset gaps), but CA1416 only
  sees compiled metadata, not source — so desktop-only API usage *inside* the toolkit is
  **not** caught. Treat a flagless build as a restore probe only, not a clean bill.

The managed compile runs on any OS with the workload installed; a full iOS *link* still
requires a Mac with Xcode.

## Why it's not in the solution

`ZGF.Gui.iOS.SmokeTest` is intentionally **excluded** from `ENV Game Framework.sln` so
that a normal desktop `dotnet build` of the solution does not require the iOS workload.
Build it explicitly (as above), or wire it into a dedicated CI job that sets
`-p:BuildIos=true`.

## Expected follow-up

When this project fails to restore/compile, that failure is the to-do list for the iOS
port (e.g. "add an iOS native asset for package X", "provide a FreeType iOS native").
See the mobile-rendering notes for the full sequence.
