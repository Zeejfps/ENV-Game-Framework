# ZgfCef interop generator (CefGlue fork for CEF 150)

Vendored from [OutSystems/CefGlue](https://github.com/OutSystems/CefGlue)
`CefGlue.Interop.Gen` (which itself descends from CEF's own `translator.py`
machinery), with the minimum changes needed to regenerate the P/Invoke interop
against **CEF 150** instead of the frozen-at-120 upstream.

Upstream CefGlue is pinned at `CEF_VERSION_MAJOR = 120`. CEF introduced its
**API-versioning** system after 120 (≈127), which the 120-era generator predates.
The changes here are a clean upstream-PR candidate. [`cef150-generator.patch`](cef150-generator.patch)
captures fixes 1–3; **fix 4 (`get_capi_name`) is in the generator source but not yet in the
patch — regenerate the patch against upstream before opening a PR.** The changes are:

1. **`cef_parser.py` — API-version resolution.** A minimal C-preprocessor pass
   (`resolve_api_version`) runs before member extraction and evaluates every
   `#if CEF_API_ADDED(v)` / `CEF_API_REMOVED(v)` gate against a pinned
   `CEF_API_VERSION` (default **15000** = `CEF_API_VERSION_LAST` for this payload;
   override via the `CEF_API_VERSION` env var). Members gated to `EXPERIMENTAL`/
   `NEXT` or a version above the target are stripped, so the emitted vtables and
   structs are byte-exact for the release `libcef.dll` ABI. Without this the
   generator would blindly include experimental members the release DLL never
   exposes → corrupt vtable → crash.

2. **`make_interop.py` — `make_version_cs`.** CEF 127+ removed the single
   `CEF_API_HASH_UNIVERSAL`/`CEF_API_HASH_PLATFORM` `#define`s from
   `cef_api_hash.h` and moved per-version, per-platform hashes into
   `cef_api_versions.h` (`CEF_API_HASH_<version>`). `make_version_cs` now reads
   the pinned version's WIN/MAC/LINUX hash from there and also emits a
   `CEF_API_VERSION` constant for the runtime version guard.

3. **`schema_cef3.py` — CEF 150 class roles.** 8 classes new in 150 were added to
   the role map, classified by each header's `allocated client-side`
   (`ROLE_HANDLER`, we implement) vs `allocated DLL-side` (`ROLE_PROXY`, CEF
   implements) note:
   - handlers: `CefComponentUpdateCallback`, `CefPreferenceObserver`, `CefSettingObserver`
   - proxies: `CefComponent`, `CefComponentUpdater`, `CefTaskManager`,
     `CefUnresponsiveProcessCallback`, `CefV8BackingStore`

   (22 classes removed in 150 remain as harmless stale map entries — the validator
   only fails on header classes *missing* from the map.)

4. **`cef_parser.py` — `get_capi_name` digit→uppercase separator.** CEF's C API name
   generator changed at 150 to insert a `_` between a digit and a following uppercase
   letter: `CefV8Value` → `cef_v8_value_t` (was `cef_v8value_t`), `CefX509Certificate`
   → `cef_x509_certificate_t`. This name feeds the P/Invoke `EntryPoint` for **exported
   global functions** (`cef_v8_value_create_object`, …), so the old rule produced
   `EntryPointNotFoundException` → render-process crash the first time a global (not
   vtable) function is called — which for our subset is the V8 bridge (Phase 5). Vtable
   methods are invoked by pointer, so this was invisible through Phase 4. Fix: add one
   condition to `get_capi_name` (underscore when the previous char is a digit). After a
   future rule change, regenerate, delete stale old-name `*.g.cs`, and re-run the sed
   over hand-written type/method references (also affects internal names like
   `base64encode`→`base64_encode`, `get_v8context`→`get_v8_context`).

## Run

```powershell
# 1. stage headers (once):  ../fetch-cef.ps1
# 2. regenerate into the vendored managed lib:
./regen.ps1                       # target ../Xilium.CefGlue, CEF_API_VERSION=15000
./regen.ps1 -ApiVersion 14600     # pin a different API version
```

Requires Python 3 on PATH. Emits ~136 `*.g.cs` interop files. Provenance patch is
regenerated with `git diff` against the upstream clone; keep it in sync when the
generator is touched so the changes stay a viable CefGlue PR.
