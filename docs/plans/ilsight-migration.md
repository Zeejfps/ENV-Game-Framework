# ILSight Migration

Move `ZGF.ILSpy` out of this repo into its own repo as **ILSight**, then add Velopack updates and an About page.
Start once the next few features have landed.

## Identity

| Thing | Value |
|---|---|
| Product name | ILSight |
| Tagline | A .NET assembly explorer |
| GitHub repo | `Zeejfps/ILSight` (PascalCase, like `Zeejfps/PngSharp`) |
| Site | `ilsight.com`, fallback `ilsight.builtbyzee.com` |
| Update feed | GitHub Releases on `Zeejfps/ILSight` |
| Velopack `packId` | `ILSight` (permanent: names the install folder `%LocalAppData%\ILSight`, the `.app` and the main exe) |
| Velopack `packTitle` | `ILSight` |
| macOS bundle id | `com.builtbyzee.ilsight`, passed explicitly; Velopack's default would be `com.ILSight.ILSight` |
| Assembly / exe name | `ILSight` |
| Root namespace | `ILSight` |
| Settings folder | `ApplicationData/ILSight` |

## Decisions

1. **Keep history.** Extract with `git-filter-repo`, as was done for `cs_games_sandbox`. That keeps blame for all 256 commits and costs the same as a copy.
2. **Consume ZGF as a submodule at `ZGF/`.** Don't copy the framework. This is the Sandbox setup: copying was rejected there because a frozen copy drifts. Framework fixes are made in the nested `ZGF/` checkout and pushed from there.
3. **Public repo.** Updates are served from GitHub Releases, so no feed hosting is needed. The licence is still open.
4. **Code signing.** Still open. macOS needs a Developer ID ($99/yr) and notarization, or downloaded builds are quarantined. Windows works unsigned but SmartScreen warns; Azure Trusted Signing is the cheap fix.

## Phase 1 — Extract

1. Pick the ZGF commit to split from and note its SHA.
2. Throwaway clone: `git clone --no-local <this repo> ilsight-extract`.
3. `git lfs fetch --all origin` first. The fonts and test-fixture DLLs are LFS objects, and Sandbox's first push failed without this.
4. Filter:
   ```
   git filter-repo \
     --path ZGF.ILSpy/ --path ZGF.ILSpy.Core/ --path ZGF.ILSpy.Tests/ --path ZGF.ILSpy.App.Tests/ \
     --path docs/plans/ilspy.md --path docs/plans/ilspy-feature-gaps.md --path docs/plans/ilspy-improvements.md \
     --path docs/plans/ilsight-migration.md --path docs/plans/ilsight-icon.md
   ```
5. `git submodule add https://github.com/Zeejfps/ENV-Game-Framework.git ZGF`, check out the SHA from step 1, then `git submodule update --init --recursive` for the nested PngSharp.
6. Add root files:
   - `global.json`: copy.
   - `Directory.Build.props`: `Nullable`, `ImplicitUsings`, MinVer. No submodule guard is needed: MSBuild stops at the nearest props file, so `ZGF/` keeps its own.
   - `Directory.Build.targets`: copy both targets. `LoadLocalAnalyzersFromCopies` is needed for `ZGF.Gui.Generator`, and `PublishRuntimeLicenses` for the AOT publish.
   - `Directory.Packages.props`: only what ILSight uses.
   - `.gitattributes`: the LFS rules for `*.ttf` and `*.dll`.
   - `.gitignore`, `CLAUDE.md`, `docs/code-readability.md`: copy.
   - `README.md`, `LICENSE`: new.
7. Point the `ProjectReference`s at `ZGF\...`. Create `ILSight.sln` with the four app projects plus the ZGF projects they reference.
8. Build and run both test projects. Push, then run `git lfs push --all origin`.

## Phase 2 — Rename (one commit)

- `git mv` the four project folders and csproj files to `ILSight`, `ILSight.Core`, `ILSight.Tests`, `ILSight.App.Tests`.
- Rename namespace `ZGF.ILSpy` → `ILSight` across all files, along with `InternalsVisibleTo` and the test `Using`.
- Rename the app's own types: `IlSpyApp`, `ILSpyAppLauncher`.
- **Keep the names that refer to the real ILSpy:** `ILSpyBookmarkInfo` and `ILSpyFile` (bookmark import), `LICENSE-ILSpy.txt`. Review every remaining `ILSpy` hit by hand rather than replacing them all.
- Change these strings:
  - `WindowTitle` → `ILSight`
  - settings folder → `ILSight`
  - `BookmarkFile.SuggestedName` → `ILSight.Bookmarks.json`
- Set `Product`, `Company` (`builtbyzee`) and `AssemblyName` in the app csproj.
- Move your own `settings.json` to the new folder by hand. There are no other users, so no migration code.
- Update the examples in `docs/code-readability.md` (`IlSpyApp` etc.).
- Tag `v0.1.0` so MinVer stops producing `0.0.0-alpha`.

## Phase 3 — Remove from this repo

Only once ILSight builds green on its own.

- Delete the four projects, the three `docs/plans/ilspy*.md` files, this file, `ilsight-icon.md` and `builds/ZGF.ILSpy`.
- Remove the projects from `ENV Game Framework.sln`.
- Swap the ILSpy examples in this repo's `docs/code-readability.md` for neutral ones, or keep that doc ILSight-only.
- Check that nothing left in ZGF references them: `ZGF.Gui.Immediate.Testing` stays here, since ILSight consumes it.

## Phase 4 — Velopack

1. Add the `Velopack` package. It is AOT-compatible on net8+.
2. The first step in `Program.cs` is `VelopackApp.Build().Run();`. It handles install/update hooks and may exit.
3. Add an `Updates` feature file:
   - check on startup when the app is installed, and skip when run from the IDE;
   - download in the background;
   - queue a `ViewCommand` that offers "Restart to update", then apply and restart;
   - add a settings toggle for automatic checks.
4. Feed: `GithubSource` pointed at `Zeejfps/ILSight`.
5. CI (`.github/workflows/release.yml`), triggered by a `v*` tag:
   - **Runners:** a matrix of `windows-latest`, `macos-latest` and `ubuntu-latest`. NativeAOT can't cross-compile between OSes.
   - **Checkout:** `submodules: recursive` and `lfs: true`; the GLFW natives inside `ZGF/` are LFS objects. Watch the LFS bandwidth quota.
   - **Build:** `dotnet publish -c Release -r <rid>`.
   - **Pack:**
     - all OSes: `vpk pack --packId ILSight --packTitle ILSight --packVersion <MinVer> --mainExe <exe> --icon <icon>`;
     - macOS also: `--bundleId com.builtbyzee.ilsight` plus signing and notarization;
     - Linux: produces an AppImage, which needs FUSE on the user's machine. The existing `build_linux-x64.dockerfile` can pin an older glibc.
   - **Upload:** `vpk upload github --publish`.
   - **Versions:** keep the `vpk` tool and the `Velopack` package on the same version.
6. RIDs: `win-x64`, `osx-arm64` (add `osx-x64` if needed), `linux-x64`.
7. Icons: `.ico`, `.icns`, `.png`, designed in [ilsight-icon.md](ilsight-icon.md).

## Phase 5 — About page

- Opened from a new Help menu: About ILSight.
- Shows:
  - app icon, name and version (`AssemblyInformationalVersion` from MinVer);
  - a "Check for updates" button with its status;
  - links to the site, repo and issues;
  - "Built by Zee";
  - "Powered by ICSharpCode.Decompiler {version}. Not affiliated with the ILSpy project.";
  - third-party notices, listing the files in `licenses/` next to the exe.
- Notices missing today:
  - Inter (OFL; only `Inter-Italic.ttf` ships, with no licence file)
  - Velopack (MIT)
  - GLFW (zlib)
  - FreeType (FTL)
  - HarfBuzz / HarfBuzzSharp (MIT)
  - FreeTypeSharp (MIT)
  - ICSharpCode.BamlDecompiler (MIT)

  Audit the publish folder for any others.
- Strings for all nine locales.
- Follows the readability rules: an `About` feature, `AboutDialog` on `UiState`, and `AboutDialogView`.
