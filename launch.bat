@echo off
setlocal
cd /d "%~dp0"
powershell.exe -ExecutionPolicy Bypass -NoProfile -File "%~dp0bootstrap.ps1"
