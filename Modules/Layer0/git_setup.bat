@echo off
title Heaplit GitHub Setup Wizard
echo ===================================================
echo             HEAPLIT GITHUB SETUP WIZARD             
echo ===================================================
echo.

:: 1. Check if git is installed
where git >nul 2>nul
if %errorlevel% neq 0 (
    echo ❌ [ERROR] Git is not installed on this system!
    echo Please install Git and try again.
    echo.
    pause
    exit
)

:: 2. Check/Initialize Repository
if not exist ".git" (
    echo ⚡ [INFO] Git repository not initialized in this directory.
    echo Initializing local repository...
    git init
    echo.
) else (
    echo ✅ [INFO] Git repository is already initialized.
    echo.
)

:: 3. Configure Git Identity
echo --- Configure Git Identity ---
echo (Leave blank to keep your current configurations)
set /p git_name="Enter your Git User Name (e.g. Kyle): "
if not "%git_name%"=="" (
    git config --global user.name "%git_name%"
    echo Saved User Name: %git_name%
)
set /p git_email="Enter your Git Email Address: "
if not "%git_email%"=="" (
    git config --global user.email "%git_email%"
    echo Saved User Email: %git_email%
)
echo.

:: 4. GitHub Authentication Setup
echo --- GitHub Authentication Setup ---
echo Heaplit will help register your credentials for automatic syncing.
echo.
echo Select Auth Mode:
echo [1] Git Credential Manager Login (Recommended - Browser Popup)
echo [2] GitHub CLI Auth (gh auth login)
echo [3] Skip Auth Setup (Use existing settings)
set /p auth_choice="Enter choice (1-3): "

if "%auth_choice%"=="1" (
    echo.
    echo Initializing Git Credential Manager...
    git config --global credential.helper manager
    echo Handshaking authentication...
    git credential-manager github login
)
if "%auth_choice%"=="2" (
    echo.
    where gh >nul 2>nul
    if %errorlevel% equ 0 (
        gh auth login
    ) else (
        echo ⚠️ [WARN] GitHub CLI ('gh') is not installed on your system PATH.
        echo Please install GitHub CLI or choose Option 1.
    )
)
echo.

:: 5. Remote Repository Linkage
echo --- Link Remote Repository ---
git remote -v
echo.
set /p link_choice="Do you want to link or change your GitHub Remote Repository? (y/n): "
if /i "%link_choice%"=="y" (
    set /p repo_url="Enter GitHub Repo URL (e.g. https://github.com/Username/Repo.git): "
    if not "%repo_url%"=="" (
        git remote remove origin >nul 2>nul
        git remote add origin "%repo_url%"
        echo.
        echo ✅ Remote 'origin' linked successfully to: %repo_url%
    )
)
echo.

:: 6. Push Default Branch Setup
echo --- Set Default Branch Name ---
set /p branch_name="Enter default branch name (Press Enter for 'main'): "
if "%branch_name%"=="" set branch_name=main
git branch -M %branch_name%
echo Branch renamed to '%branch_name%'.
echo.

echo ===================================================
echo  🎉 GITHUB CONFIGURATION COMPLETED SUCCESSFULLY!
echo ===================================================
echo You can now use the 'push' command inside Heaplit.
echo.
pause
