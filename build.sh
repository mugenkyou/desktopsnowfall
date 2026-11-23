#!/bin/zsh
# Build and run desktopsnowfall
cd "$(dirname "$0")"
swiftc -O desktopsnowfall.swift -o desktopsnowfall || exit 1
echo "Build succeeded ❄︎  Use the ❄︎ menu bar icon to pause, change the snow amount or quit"
./desktopsnowfall
