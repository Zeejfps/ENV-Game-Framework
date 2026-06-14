#!/usr/bin/env bash
#
# Builds a minimal FreeType static library for the browser-wasm runtime so the
# ZGF GUI font backend (ZGF.Fonts.FreeTypeFontBackend) can rasterize glyphs in
# the browser under .NET WebAssembly. See docs/web-font-rendering.md.
#
# Output:  native/wasm/libfreetype.a   (a browser-wasm static archive)
#
# Why a custom build: FreeTypeSharp ships native assets for win/linux/osx/
# android/ios but NOT browser-wasm. HarfBuzz (shaping) is covered by the
# published HarfBuzzSharp.NativeAssets.WebAssembly package; FreeType
# (rasterization + metrics) is the one piece we own.
#
# Requirements:
#   - The Emscripten SDK at the EXACT version .NET's wasm-tools workload links
#     with (see find_dotnet_emsdk_version below). A mismatched emsdk produces an
#     archive that fails to link into the .NET wasm app.
#   - cmake, make, curl, shasum/sha256sum, tar.
#
# Usage:
#   tools/build-freetype-wasm.sh            # build if missing/stale
#   FORCE=1 tools/build-freetype-wasm.sh    # always rebuild
#
set -euo pipefail

# ----------------------------------------------------------------------------
# Pins. Bump these deliberately; record the working combination in
# docs/web-font-rendering.md when the .NET SDK changes.
# ----------------------------------------------------------------------------

# FreeType 2.13.x matches the FreeTypeSharp 3.0.1 P/Invoke ABI used by ZGF.Fonts.
FREETYPE_VERSION="${FREETYPE_VERSION:-2.13.3}"
# sha256 of freetype-${FREETYPE_VERSION}.tar.xz from https://download.savannah.gnu.org/releases/freetype/
# Verify and update this when bumping FREETYPE_VERSION.
FREETYPE_SHA256="${FREETYPE_SHA256:-0550350666d427c74daeb85d5ac7bb353acba5f76956395995311a9c6f063289}"

# Override to skip auto-detection (e.g. in CI with a preinstalled emsdk).
EMSDK_VERSION="${EMSDK_VERSION:-}"

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
BUILD_ROOT="${ROOT_DIR}/.build/freetype-wasm"
OUT_DIR="${ROOT_DIR}/native/wasm"
OUT_LIB="${OUT_DIR}/libfreetype.a"
STAMP="${OUT_DIR}/.libfreetype.a.stamp"

log() { printf '\033[36m[freetype-wasm]\033[0m %s\n' "$*"; }
die() { printf '\033[31m[freetype-wasm] ERROR:\033[0m %s\n' "$*" >&2; exit 1; }

sha256_of() {
  if command -v sha256sum >/dev/null 2>&1; then sha256sum "$1" | awk '{print $1}';
  elif command -v shasum >/dev/null 2>&1; then shasum -a 256 "$1" | awk '{print $1}';
  else die "need sha256sum or shasum"; fi
}

# ----------------------------------------------------------------------------
# Discover the Emscripten version pinned by the installed .NET wasm-tools
# workload. The version is encoded in the Microsoft.NET.Runtime.Emscripten.*
# runtime pack name pulled in by the workload.
# ----------------------------------------------------------------------------
find_dotnet_emsdk_version() {
  [ -n "${EMSDK_VERSION}" ] && { echo "${EMSDK_VERSION}"; return; }

  # Resolve the SDK root from the dotnet binary's real location so this works
  # regardless of install prefix (/usr/local/share/dotnet, /usr/share/dotnet, ...).
  local dotnet_root=""
  if command -v dotnet >/dev/null 2>&1; then
    dotnet_root="$(cd "$(dirname "$(readlink "$(command -v dotnet)" || command -v dotnet)")" && pwd)"
  fi

  local pack
  pack="$(find "${HOME}/.dotnet" "${DOTNET_ROOT:-/usr/share/dotnet}" "${dotnet_root:-/usr/share/dotnet}" "${HOME}/.nuget" \
            -maxdepth 6 -type d -iname 'Microsoft.NET.Runtime.Emscripten.*.Node.*' 2>/dev/null \
          | head -n1 || true)"
  [ -z "${pack}" ] && return 1

  # .../Microsoft.NET.Runtime.Emscripten.<VER>.Node.<rid>/...  -> extract <VER>
  basename "${pack}" | sed -E 's/^Microsoft\.NET\.Runtime\.Emscripten\.([0-9.]+)\.Node\..*$/\1/'
}

# ----------------------------------------------------------------------------
# Staleness check: skip the build if the archive exists and the pins are
# unchanged (recorded in the stamp).
# ----------------------------------------------------------------------------
want_stamp="freetype=${FREETYPE_VERSION} emsdk=${EMSDK_VERSION:-auto}"
if [ -z "${FORCE:-}" ] && [ -f "${OUT_LIB}" ] && [ -f "${STAMP}" ] \
   && [ "$(cat "${STAMP}")" = "${want_stamp}" ]; then
  log "up to date: ${OUT_LIB}"
  exit 0
fi

command -v cmake >/dev/null 2>&1 || die "cmake not found"
command -v curl  >/dev/null 2>&1 || die "curl not found"

# ----------------------------------------------------------------------------
# Ensure an active Emscripten toolchain (emcc on PATH). If emcc isn't present,
# provision a pinned emsdk into .build/emsdk.
# ----------------------------------------------------------------------------
if ! command -v emcc >/dev/null 2>&1; then
  ver="$(find_dotnet_emsdk_version || true)"
  [ -z "${ver}" ] && die "Could not detect .NET's pinned Emscripten version. \
Install the workload (dotnet workload install wasm-tools) or set EMSDK_VERSION explicitly."
  log "provisioning emsdk ${ver} (matches .NET wasm-tools)"
  EMSDK_DIR="${BUILD_ROOT}/emsdk"
  if [ ! -d "${EMSDK_DIR}" ]; then
    git clone --depth 1 https://github.com/emscripten-core/emsdk.git "${EMSDK_DIR}"
  fi
  ( cd "${EMSDK_DIR}" && ./emsdk install "${ver}" && ./emsdk activate "${ver}" )
  # shellcheck disable=SC1091
  source "${EMSDK_DIR}/emsdk_env.sh"
fi
command -v emcc >/dev/null 2>&1 || die "emcc still not on PATH after emsdk activation"
log "using $(emcc --version | head -n1)"

# ----------------------------------------------------------------------------
# Fetch + verify FreeType source.
# ----------------------------------------------------------------------------
mkdir -p "${BUILD_ROOT}"
SRC_DIR="${BUILD_ROOT}/freetype-${FREETYPE_VERSION}"
TARBALL="${BUILD_ROOT}/freetype-${FREETYPE_VERSION}.tar.xz"

if [ ! -d "${SRC_DIR}" ]; then
  if [ ! -f "${TARBALL}" ]; then
    log "downloading FreeType ${FREETYPE_VERSION}"
    curl -fSL --retry 4 \
      "https://download.savannah.gnu.org/releases/freetype/freetype-${FREETYPE_VERSION}.tar.xz" \
      -o "${TARBALL}"
  fi
  got="$(sha256_of "${TARBALL}")"
  [ "${got}" = "${FREETYPE_SHA256}" ] \
    || die "FreeType tarball checksum mismatch: got ${got}, want ${FREETYPE_SHA256}"
  log "checksum OK; extracting"
  tar -C "${BUILD_ROOT}" -xf "${TARBALL}"
fi

# ----------------------------------------------------------------------------
# Configure a minimal FreeType: outline rasterization + metrics + embolden only.
# Every optional dependency is disabled so the archive is self-contained (no
# zlib/png/brotli/harfbuzz). TrueType/CFF/autohint drivers stay enabled — they
# are what produce our desktop anti-aliasing.
#
# -sSUPPORT_LONGJMP=wasm: FreeType's smooth rasterizer (ftgrays.c) uses
# setjmp/longjmp. The .NET wasm runtime links with wasm-style SjLj
# (-mllvm -wasm-enable-sjlj), so the archive must use the SAME longjmp ABI;
# the Emscripten default (JS-based) emits an `emscripten_longjmp` symbol that
# the .NET link does not provide, which fails wasm-ld with an undefined symbol.
# ----------------------------------------------------------------------------
CMAKE_BUILD="${BUILD_ROOT}/build-${FREETYPE_VERSION}"
log "configuring (emcmake)"
emcmake cmake -B "${CMAKE_BUILD}" -S "${SRC_DIR}" \
  -DCMAKE_BUILD_TYPE=Release \
  -DBUILD_SHARED_LIBS=OFF \
  -DFT_DISABLE_ZLIB=ON \
  -DFT_DISABLE_BZIP2=ON \
  -DFT_DISABLE_PNG=ON \
  -DFT_DISABLE_BROTLI=ON \
  -DFT_DISABLE_HARFBUZZ=ON \
  -DCMAKE_C_FLAGS="-O2 -fPIC -sSUPPORT_LONGJMP=wasm"

log "building"
emmake make -C "${CMAKE_BUILD}" -j"$(getconf _NPROCESSORS_ONLN 2>/dev/null || echo 4)"

BUILT="$(find "${CMAKE_BUILD}" -name 'libfreetype.a' | head -n1)"
[ -z "${BUILT}" ] && die "build produced no libfreetype.a"

mkdir -p "${OUT_DIR}"
cp "${BUILT}" "${OUT_LIB}"
echo "${want_stamp}" > "${STAMP}"
log "done -> ${OUT_LIB}"
log "      ($(du -h "${OUT_LIB}" | awk '{print $1}'))"
