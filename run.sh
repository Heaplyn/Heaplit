#!/bin/bash
# Developer: heaplyn
# Date: 2026-08-09
# Summary: Cleans, builds, and launches the Heaplit Launcher WPF application.

# Get the directory of the active script
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

echo "🧹 Cleaning previous build caches..."
dotnet clean

echo "⚙️ Building Heaplit Launcher..."
dotnet build

echo "🚀 Launching Heaplit HUD background service..."
# Run build target silently without rebuilding since we just ran dotnet build
dotnet run --no-build
