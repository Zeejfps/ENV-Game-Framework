#!/usr/bin/env bash
# macOS counterpart of fetch-cef.ps1: fetch, SHA1-verify and stage the pinned CEF build (from
# CEF_VERSION) into cef/<rid>/{include,Release}. The runtime is Release/Chromium Embedded Framework.framework.
# Usage: ./fetch-cef.sh [--rid osx-arm64|osx-x64] [--distribution minimal|client] [--force]

set -euo pipefail

root="$(cd "$(dirname "$0")" && pwd)"

case "$(uname -m)" in
    arm64) rid=osx-arm64 ;;
    *) rid=osx-x64 ;;
esac
distribution=minimal
force=0

while [ $# -gt 0 ]; do
    case "$1" in
        --rid) rid="$2"; shift 2 ;;
        --distribution) distribution="$2"; shift 2 ;;
        --force) force=1; shift ;;
        *) echo "unknown argument: $1" >&2; exit 2 ;;
    esac
done

if [ "$(uname -s)" != "Darwin" ]; then
    echo "fetch-cef.sh stages macOS payloads only; on Windows run fetch-cef.ps1" >&2
    exit 1
fi

# --- read the pin -----------------------------------------------------------
version_file="$root/CEF_VERSION"
[ -f "$version_file" ] || { echo "CEF_VERSION not found at $version_file" >&2; exit 1; }
pin() { sed -n "s/^[[:space:]]*$1[[:space:]]*=[[:space:]]*\(.*[^[:space:]]\)[[:space:]]*$/\1/p" "$version_file"; }
cef_version="$(pin CEF_VERSION)"
[ -n "$cef_version" ] || { echo "CEF_VERSION= not set in $version_file" >&2; exit 1; }

# --- map rid/distribution -> CDN file + expected sha1 -----------------------
case "$rid" in
    osx-arm64) cdn_platform=macosarm64 ;;
    osx-x64) cdn_platform=macosx64 ;;
    *) echo "unsupported rid: $rid (osx-arm64 | osx-x64)" >&2; exit 2 ;;
esac
case "$distribution" in
    minimal|client) ;;
    *) echo "unsupported distribution: $distribution (minimal | client)" >&2; exit 2 ;;
esac
sha_key="SHA1_$(echo "${cdn_platform}_${distribution}" | tr '[:lower:]' '[:upper:]')"
expected_sha1="$(pin "$sha_key")"
[ -n "$expected_sha1" ] || { echo "$sha_key not set in $version_file" >&2; exit 1; }

file_name="cef_binary_${cef_version}_${cdn_platform}_${distribution}.tar.bz2"
# The '+' chars in the version are %2B in the URL.
url="https://cef-builds.spotifycdn.com/${file_name//+/%2B}"

dl_dir="$root/.cef-download"
mkdir -p "$dl_dir"
archive="$dl_dir/$file_name"

# --- download (skip if cached + valid) --------------------------------------
sha1_of() { shasum -a 1 "$1" | cut -d' ' -f1; }

if [ -f "$archive" ] && [ "$force" -eq 0 ] && [ "$(sha1_of "$archive")" = "$expected_sha1" ]; then
    echo "Using cached $file_name (sha1 ok)"
else
    echo "Downloading $url"
    curl -fL --progress-bar -o "$archive" "$url"
    actual="$(sha1_of "$archive")"
    if [ "$actual" != "$expected_sha1" ]; then
        echo "SHA1 mismatch for $file_name" >&2
        echo "  expected $expected_sha1" >&2
        echo "  actual   $actual" >&2
        exit 1
    fi
    echo "SHA1 verified: $actual"
fi

# --- extract ----------------------------------------------------------------
extract_dir="$dl_dir/extract-$rid-$distribution"
rm -rf "$extract_dir"
mkdir -p "$extract_dir"
echo "Extracting..."
tar -xjf "$archive" -C "$extract_dir"
payload="$(find "$extract_dir" -mindepth 1 -maxdepth 1 -type d | head -n 1)"
[ -n "$payload" ] || { echo "no payload directory after extraction" >&2; exit 1; }

# --- stage into cef/<rid>/{include,Release} ---------------------------------
stage="$root/cef/$rid"
rm -rf "$stage"
mkdir -p "$stage"

for sub in include Release Resources; do
    if [ -d "$payload/$sub" ]; then
        # ditto keeps the framework's symlinks and extended attributes intact.
        ditto "$payload/$sub" "$stage/$sub"
        echo "  staged $sub/"
    fi
done

echo "CEF $cef_version ($distribution) staged at $stage"
