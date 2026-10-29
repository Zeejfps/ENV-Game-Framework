# Phase 0 — GO/NO-GO gate result

**Verdict: GO.** Chromium 150 OSR does **not** reproduce the CEF-120 render crash.

Run on 2026-07-21, Windows 11 x64, against the pinned build
`150.0.14+g7c1aa68+chromium-150.0.7871.129` (see [CEF_VERSION](CEF_VERSION)).

## Probe

Used the prebuilt `cefclient.exe` from the CEF 150 **client** distribution (the
OSR-capable sample; `cefsimple` is windowed-only) in off-screen mode:

```
cefclient.exe --off-screen-rendering-enabled --url=<URL> \
              --enable-logging=stderr --v=1 --windowless-frame-rate=30
```

on both `https://www.google.com` and `https://run.world`, ~20 s each, stderr captured.

## Result

| Page | Render process | CEF-120 crash signature? |
|---|---|---|
| google.com | alive (6-process tree) | none |
| run.world  | alive (8-process tree) | none |

Decisive signal on **run.world**: the render process executed the page's own
JavaScript bundle and logged application console output
(`INFO:CONSOLE ... "Web platform detected. Using RevenueCat..."`,
`"Service Worker registered with scope: https://run.world/"`) — the renderer
loaded and ran the Expo/React SPA and kept compositing. This is the exact class
of open-web page that crashed the CEF-120 renderer with:

```
[ERROR:shared_image_manager.cc(220)] SharedImageManager::ProduceSkia:
  Trying to Produce a Skia representation from a non-existent mailbox.
→ render process CRASHED
```

No `shared_image_manager` / `ProduceSkia` / `CRASHED` markers appeared on 150.

Only benign noise was logged: HKLM registry access-denied warnings, a
`fallback_task_provider` warning (known-benign cef/chromium), and speculative-frame
warnings. None are rendering faults.

**Conclusion:** the Chromium-150 OSR compositor is healthy → proceed to build the
binding against this exact payload.
