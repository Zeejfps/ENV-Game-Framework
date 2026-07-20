# GLFW natives

These four binaries are **not stock GLFW**. They carry the IME/preedit patch
([glfw/glfw#2130](https://github.com/glfw/glfw/pull/2130)), which stock GLFW does not have and
which `ZGF.Gui.Desktop`'s composition input path requires — without it `GlfwIme.IsSupported`
returns false and CJK text cannot be typed (see `docs/plans/cjk-ime-support.md` in the GitBench
repo). They additionally export `glfwSetTextInputFocus`, which scopes the IME to focused text
fields; without it every bare-letter keyboard shortcut is dead while a CJK IME is active (see
`docs/plans/done/ime-text-input-focus.md`).

Do not replace them with a distro package or an upstream GLFW release archive. Both are unpatched,
and the regression is silent: everything still builds and runs, CJK input just stops working.

| RID | File | Import name | Size | SHA-256 |
| --- | --- | --- | --- | --- |
| win-x64 | `glfw3.dll` | `glfw3` | 400,896 | `2324a13e22888de1302c9f172cb8b1798b23b6e5a8afa4c43b20122c840e1460` |
| linux-x64 | `libglfw.so.3` | soname, resolved by `NativeLibraryResolver` | 467,104 | `d83497d1e1a78be3a7794d9de33eda84468d634205416d57dc65372a8ab6c475` |
| osx-x64 | `libglfw.3.dylib` | `glfw3` | 527,904 | `ca7c93791395b3fbf43a05a01ab13c335bf0890acf21fc201b04f823221bc64b` |
| osx-arm64 | `libglfw.3.dylib` | `glfw3` | 527,904 | `ca7c93791395b3fbf43a05a01ab13c335bf0890acf21fc201b04f823221bc64b` |

The two macOS entries are the same universal (x86_64 + arm64) binary, by design — see below.

## Provenance

Built from source by `.github/workflows/glfw-natives.yml`, from a pinned commit of the fork that
carries the IM-support patch:

```
clear-code/glfw   9af719e6073a317f2dfa17f6cf0c373619b1ec8c   (branch im-support, reports version 3.5.0)
```

The branch is a moving PR head, so the workflow takes an exact SHA and never a branch name.

These previously came from [LWJGL](https://github.com/LWJGL/lwjgl3) 3.3.4's GLFW jars, built from
`b35641f4a3c62aa86a0b3c983d163bc0fe36026d`. That route was abandoned because **no LWJGL release
exports `glfwSetTextInputFocus`** — their fork is pinned to an im-support commit older than the
function, so upgrading LWJGL could never deliver it. The pinned commit above is 108 commits ahead
of `b35641f` and 0 behind, so it is a strict superset of what previously shipped.

## Rebuilding

Dispatch the **glfw-natives** workflow (Actions → glfw-natives → Run workflow), optionally
overriding the repo/SHA inputs. It builds all four on their native runners, then publishes one
artifact containing the binaries plus a `MANIFEST.md` with the checksum table and the drift log
against the commit above.

Download the artifact, drop each file into its RID folder here, and update the table above from
`MANIFEST.md`. The workflow deliberately does not commit anything: a native bump is an act, not a
side effect of a commit.

**Check what disappeared.** The pinned SHA guarantees which symbols are *present*, not which are
*gone*, and a removed export that we P/Invoke surfaces as a runtime `EntryPointNotFoundException`
that no test covers. Dump the exports of the old and new binary and read the difference before
committing. The move off LWJGL removed six exports —
`_glfw_{egl,mesa,opengl,opengles,vulkan}_library` and `glfwAttachWin32Window` — all LWJGL fork
additions absent from upstream, and none referenced here.

## Platform notes

### Linux — X11 vs Wayland

The Linux native is built with **both Wayland and X11** backends, and GLFW 3.4+ binds to Wayland
whenever the session offers one — which is the default on current Ubuntu and Fedora. Wayland does
not let a client position a window, read back its position, focus itself, or set an icon; all four
return `GLFW_FEATURE_UNAVAILABLE`, and popups, context menus and secondary windows are built on
them.

`Glfw`'s static constructor therefore pins Linux to X11 (XWayland under a Wayland session) via the
`GLFW_PLATFORM` init hint. Do not remove that without making the window system Wayland-aware first.

The build passes `-DGLFW_BUILD_X11=ON -DGLFW_BUILD_WAYLAND=ON` explicitly rather than letting CMake
auto-detect. That is the guard, not boilerplate: GLFW hard-errors on a missing backend dependency,
so a half-provisioned runner fails the build instead of quietly producing an X11-only native.

### macOS — both slots are universal

`osx-x64/` and `osx-arm64/` hold the **same** universal binary, produced in one build via
`-DCMAKE_OSX_ARCHITECTURES="x86_64;arm64"`. Keep it that way. A thin dylib compiles, links and
packages perfectly and only fails at `dlopen` under the other architecture — and the csproj's
RID-less macOS fallback bundles the `osx-x64` file, so a thin x86_64 build there breaks local
development on Apple Silicon. Universal in both slots removes the failure mode rather than relying
on the csproj conditions to dodge it.

Deployment target is 11.0, the floor for the arm64 slice (CMake applies one target to both).

### Windows — static CRT

Built with `-DCMAKE_MSVC_RUNTIME_LIBRARY=MultiThreaded`, matching what LWJGL shipped. A stock MSVC
build links the dynamic CRT and takes a `vcruntime140.dll` dependency, which is invisible until the
app runs on a machine without the VC++ redistributable.

## Verifying a candidate binary

`ZGF.Gui.Tests/GlfwImeNativeTests` asserts the patch is present, and is the guard against a stock
binary landing here — the failure this file exists to prevent. To inspect a binary by hand, dump its
exports (`dumpbin /exports`, `nm -D --defined-only`, `nm -gU`) and look for `glfwSetPreeditCallback`
and `glfwSetTextInputFocus`. On macOS a plain string search will not find them: Mach-O compresses
export names into a prefix trie, so the name never appears as a contiguous string. `nm -gU` reads
the trie correctly.

## Storage

All four are tracked with **Git LFS** (`*.dll`, `*.so.*`, `*.dylib` in `.gitattributes`). Note that
`*.so` does **not** match `libglfw.so.3` — the versioned soname needs `*.so.*`, which is why both
patterns are listed. Consumers cloning this repo as a submodule need LFS available, or these files
arrive as 131-byte pointer stubs that package cleanly and fail at load.

## License

GLFW is zlib-licensed, so redistribution in binary form is permitted with the license notice
retained — see `LICENSE.glfw`.
