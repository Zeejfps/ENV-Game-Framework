# GLFW natives

These four binaries are **not stock GLFW**. They carry the IME/preedit patch
([glfw/glfw#2130](https://github.com/glfw/glfw/pull/2130)), which stock GLFW does not have and
which `ZGF.Gui.Desktop`'s composition input path requires — without it `GlfwIme.IsSupported()`
returns false and CJK text cannot be typed (see `docs/plans/cjk-ime-support.md`).

Do not replace them with a distro package or an upstream GLFW release archive. Both are unpatched,
and the regression is silent: everything still builds and runs, CJK input just stops working.

| RID | File | Import name |
| --- | --- | --- |
| win-x64 | `glfw3.dll` | `glfw3` |
| linux-x64 | `libglfw.so.3` | soname, resolved by `NativeLibraryResolver` |
| osx-x64 | `libglfw.3.dylib` | `glfw3` |
| osx-arm64 | `libglfw.3.dylib` | `glfw3` |

## Provenance

Taken from [LWJGL](https://github.com/LWJGL/lwjgl3) 3.3.4, whose GLFW builds ship the IME patch.
LWJGL builds from its own GLFW fork; the upstream commit is recorded in each jar's `META-INF`:

```
GLFW commit  b35641f4a3c62aa86a0b3c983d163bc0fe36026d   (reports version 3.5.0)
```

GLFW is zlib-licensed, so redistribution in binary form is permitted with the license notice
retained — see `LICENSE.glfw`.

## Reproducing

```bash
BASE=https://repo1.maven.org/maven2/org/lwjgl/lwjgl-glfw/3.3.4/lwjgl-glfw-3.3.4
curl -sSLO $BASE-natives-windows.jar     # windows/x64/org/lwjgl/glfw/glfw.dll    -> win-x64/glfw3.dll
curl -sSLO $BASE-natives-linux.jar       # linux/x64/org/lwjgl/glfw/libglfw.so    -> linux-x64/libglfw.so.3
curl -sSLO $BASE-natives-macos.jar       # macos/x64/org/lwjgl/glfw/libglfw.dylib -> osx-x64/libglfw.3.dylib
curl -sSLO $BASE-natives-macos-arm64.jar # macos/arm64/.../libglfw.dylib          -> osx-arm64/libglfw.3.dylib
```

Take `libglfw.dylib`, **not** `libglfw_async.dylib` — the async variant exists for JVM hosts that
cannot run the event loop on the main thread, which does not apply to us.

To verify a candidate binary actually carries the patch, check that it exports
`glfwSetPreeditCallback`. On macOS a plain string search will not find it (Mach-O compresses export
names into a prefix trie) — parse the export trie, or just run the app and check
`GlfwIme.IsSupported()`.

## Version skew

These bump all platforms to one build (reported version `3.5.0`). Previously the natives were
mismatched — win 3.3.7, linux 3.3.6, macOS 3.4.0 — so this unifies them as a side effect.
