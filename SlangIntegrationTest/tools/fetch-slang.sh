#!/usr/bin/env bash
set -euo pipefail

SLANG_VERSION="${SLANG_VERSION:-2026.7.1}"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
NATIVE_DIR="$PROJECT_DIR/Native"

OS="$(uname -s)"
ARCH="$(uname -m)"

case "$OS-$ARCH" in
    Darwin-x86_64)
        ARCHIVE="slang-${SLANG_VERSION}-macos-x86_64.zip"
        TARGET_DIR="$NATIVE_DIR/osx-x64"
        SOURCE_SUBDIR="lib"
        FILE_GLOB="*.dylib"
        ;;
    Darwin-arm64)
        ARCHIVE="slang-${SLANG_VERSION}-macos-aarch64.zip"
        TARGET_DIR="$NATIVE_DIR/osx-arm64"
        SOURCE_SUBDIR="lib"
        FILE_GLOB="*.dylib"
        ;;
    Linux-x86_64)
        ARCHIVE="slang-${SLANG_VERSION}-linux-x86_64-glibc-2.17.tar.gz"
        TARGET_DIR="$NATIVE_DIR/linux-x64"
        SOURCE_SUBDIR="lib"
        FILE_GLOB="*.so*"
        ;;
    *)
        echo "fetch-slang: unsupported platform $OS-$ARCH" >&2
        exit 1
        ;;
esac

STAMP_FILE="$TARGET_DIR/.slang-version"
if [[ -f "$STAMP_FILE" ]] && [[ "$(cat "$STAMP_FILE")" == "$SLANG_VERSION" ]]; then
    exit 0
fi

URL="https://github.com/shader-slang/slang/releases/download/v${SLANG_VERSION}/${ARCHIVE}"
TMP_DIR="$(mktemp -d)"
trap 'rm -rf "$TMP_DIR"' EXIT

echo "fetch-slang: downloading Slang $SLANG_VERSION ($OS-$ARCH)"
echo "  $URL"
curl -fL --silent --show-error -o "$TMP_DIR/slang.archive" "$URL"

EXTRACT_DIR="$TMP_DIR/extract"
mkdir -p "$EXTRACT_DIR"
case "$ARCHIVE" in
    *.zip)    unzip -q "$TMP_DIR/slang.archive" -d "$EXTRACT_DIR" ;;
    *.tar.gz) tar -xzf "$TMP_DIR/slang.archive" -C "$EXTRACT_DIR" ;;
esac

# Slang archives are sometimes flat, sometimes nested in slang-X.Y.Z/.
SOURCE_DIR=""
if [[ -d "$EXTRACT_DIR/$SOURCE_SUBDIR" ]]; then
    SOURCE_DIR="$EXTRACT_DIR/$SOURCE_SUBDIR"
else
    NESTED="$(find "$EXTRACT_DIR" -maxdepth 2 -type d -name "$SOURCE_SUBDIR" | head -1)"
    [[ -n "$NESTED" ]] && SOURCE_DIR="$NESTED"
fi

if [[ -z "$SOURCE_DIR" ]]; then
    echo "fetch-slang: could not find $SOURCE_SUBDIR/ in archive" >&2
    exit 1
fi

mkdir -p "$TARGET_DIR"
shopt -s nullglob
matches=("$SOURCE_DIR"/$FILE_GLOB)
if [[ ${#matches[@]} -eq 0 ]]; then
    echo "fetch-slang: no $FILE_GLOB files in $SOURCE_DIR" >&2
    exit 1
fi

cp "${matches[@]}" "$TARGET_DIR/"
echo "$SLANG_VERSION" > "$STAMP_FILE"

echo "fetch-slang: installed ${#matches[@]} file(s) to $TARGET_DIR"
