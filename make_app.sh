#!/bin/zsh
# Package desktopsnowfall into a double-clickable Mac app + a zip ready for GitHub Releases.
# Usage: zsh make_app.sh [version]        e.g.  zsh make_app.sh 1.0
set -e
cd "$(dirname "$0")"

VERSION="${1:-1.0}"
APP="dist/desktopsnowfall.app"
ZIP="dist/desktopsnowfall-mac-v${VERSION}.zip"

echo "❄︎ Building desktopsnowfall ${VERSION}…"
rm -rf dist
mkdir -p "$APP/Contents/MacOS" "$APP/Contents/Resources" dist/tmp

# 1. Universal binary: runs on both Apple silicon and Intel Macs
swiftc -O -target arm64-apple-macos12  desktopsnowfall.swift -o dist/tmp/desktopsnowfall-arm64
swiftc -O -target x86_64-apple-macos12 desktopsnowfall.swift -o dist/tmp/desktopsnowfall-x86_64
lipo -create dist/tmp/desktopsnowfall-arm64 dist/tmp/desktopsnowfall-x86_64 -output "$APP/Contents/MacOS/desktopsnowfall"

# 2. App icon (from assets/icon.png)
ICONSET=dist/tmp/AppIcon.iconset
mkdir -p "$ICONSET"
for s in 16 32 128 256 512; do
  sips -z $s $s assets/icon.png --out "$ICONSET/icon_${s}x${s}.png" >/dev/null
  sips -z $((s*2)) $((s*2)) assets/icon.png --out "$ICONSET/icon_${s}x${s}@2x.png" >/dev/null
done
iconutil -c icns "$ICONSET" -o "$APP/Contents/Resources/AppIcon.icns"

# 3. Info.plist (LSUIElement = no Dock icon, it lives in the menu bar)
cat > "$APP/Contents/Info.plist" <<EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>CFBundleName</key>               <string>desktopsnowfall</string>
  <key>CFBundleDisplayName</key>        <string>desktopsnowfall</string>
  <key>CFBundleIdentifier</key>         <string>io.github.mugenkyou.desktopsnowfall</string>
  <key>CFBundleExecutable</key>         <string>desktopsnowfall</string>
  <key>CFBundleIconFile</key>           <string>AppIcon</string>
  <key>CFBundlePackageType</key>        <string>APPL</string>
  <key>CFBundleShortVersionString</key> <string>${VERSION}</string>
  <key>CFBundleVersion</key>            <string>${VERSION}</string>
  <key>LSMinimumSystemVersion</key>     <string>12.0</string>
  <key>LSUIElement</key>                <true/>
  <key>NSHighResolutionCapable</key>    <true/>
</dict>
</plist>
EOF

# 4. Ad-hoc signature (free; required for apps to run on Apple silicon)
codesign --force --deep --sign - "$APP"

# 5. Zip it for GitHub Releases (ditto keeps the app bundle intact)
ditto -c -k --keepParent "$APP" "$ZIP"
rm -rf dist/tmp

echo ""
echo "✅ Done!"
echo "   App: $APP   (double-click to test)"
echo "   Zip: $ZIP   (upload this to GitHub Releases)"
