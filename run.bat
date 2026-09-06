@echo off
:: Developer: heaplyn
:: Date: 2026-08-19
:: Summary: Optimized Heaplit Bootstrapper with binary auto-update logic.

cd /d "%~dp0"

echo 🔍 Checking for running instances...
taskkill /f /im HeaplitLauncher.exe >nul 2>&1

echo 🚀 Launching Heaplit HUD Environment...
dotnet run -f net10.0-windows --no-launch-profile
exit
