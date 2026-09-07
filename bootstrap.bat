@echo off
setlocal
cd /d "%~dp0"
echo ================================================================
echo             Heaplit Bootstrap & Dependency Installer            
echo ================================================================
powershell.exe -ExecutionPolicy Bypass -NoProfile -File "%~dp0bootstrap.ps1"
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo An error occurred during bootstrap. Press any key to exit...
    pause >nul
)
