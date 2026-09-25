#!/usr/bin/env bash
# Builds "Seed Placement Randomizer.app" for one macOS architecture and zips it.
# Usage: packaging/macos/package.sh <osx-arm64|osx-x64> <version> <output dir>
# Run on macOS from the repository root; codesign and ditto are macOS tools.
set -euo pipefail

rid="$1"
version="$2"
out="$3"
app="$out/Seed Placement Randomizer.app"

dotnet publish src/SeedPlacement.App -c Release -r "$rid" -o "$out/publish-$rid" -p:Version="$version"

rm -rf "$app"
mkdir -p "$app/Contents/MacOS" "$app/Contents/Resources"
cp -R "$out/publish-$rid/." "$app/Contents/MacOS/"
cp packaging/macos/icon.icns "$app/Contents/Resources/icon.icns"
sed "s/__VERSION__/$version/g" packaging/macos/Info.plist > "$app/Contents/Info.plist"
chmod +x "$app/Contents/MacOS/SeedPlacementRandomizer"

# Ad-hoc signing lets Apple silicon run the app; without a Developer ID it still needs right-click, Open.
codesign --force --deep --sign - "$app"

ditto -c -k --keepParent "$app" "$out/SeedPlacementRandomizer-$version-$rid.zip"
rm -rf "$app" "$out/publish-$rid"
