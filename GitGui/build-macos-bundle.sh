#!/usr/bin/env bash
set -euo pipefail

if [[ $# -ne 6 ]]; then
  echo "usage: $0 <publish-dir> <bundle-dir> <exe-name> <bundle-id> <display-name> <version>" >&2
  exit 2
fi

publish_dir="$1"
bundle_dir="$2"
exe_name="$3"
bundle_id="$4"
display_name="$5"
version="$6"

contents="$bundle_dir/Contents"
macos="$contents/MacOS"
resources="$contents/Resources"

rm -rf "$bundle_dir"
mkdir -p "$macos" "$resources"

rsync -a \
  --exclude='*.pdb' \
  --exclude='*.runtimeconfig.json' \
  --exclude='*.dSYM' \
  --exclude='.DS_Store' \
  "$publish_dir"/ "$macos"/

cat > "$contents/Info.plist" <<EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>CFBundleExecutable</key>
  <string>$exe_name</string>
  <key>CFBundleIdentifier</key>
  <string>$bundle_id</string>
  <key>CFBundleName</key>
  <string>$display_name</string>
  <key>CFBundleDisplayName</key>
  <string>$display_name</string>
  <key>CFBundlePackageType</key>
  <string>APPL</string>
  <key>CFBundleShortVersionString</key>
  <string>$version</string>
  <key>CFBundleVersion</key>
  <string>1</string>
  <key>LSMinimumSystemVersion</key>
  <string>11.0</string>
  <key>NSHighResolutionCapable</key>
  <true/>
  <key>NSPrincipalClass</key>
  <string>NSApplication</string>
</dict>
</plist>
EOF

chmod +x "$macos/$exe_name"

echo "Created macOS app bundle: $bundle_dir"
