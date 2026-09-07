# ==============================================================================
# Heaplit Universal Online Bootstrapper & Package Provisioner
# Auto-detects and installs missing .NET 10 SDK/Desktop Runtime, VC++ Redist,
# WebView2, Winget, and builds Heaplit automatically.
# ==============================================================================

Write-Host "================================================================" -ForegroundColor Cyan
Write-Host "         Heaplit Universal Online Package & Runtime Setup       " -ForegroundColor Yellow
Write-Host "================================================================" -ForegroundColor Cyan

# Check Administrator Elevation
 = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not ) {
    Write-Host "[i] Elevated permissions recommended. Attempting self-elevation..." -ForegroundColor Yellow
    try {
        Start-Process powershell.exe -ArgumentList "-ExecutionPolicy Bypass -NoProfile -File """ -Verb RunAs
        exit
    } catch {
        Write-Host "[!] Continuing in user mode..." -ForegroundColor Yellow
    }
}

[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12 -bor [Net.SecurityProtocolType]::Tls13
 = Split-Path -Parent 
if (-not ) {  = (Get-Location).Path }

# 1. Check and Install Visual C++ 2015-2022 Redistributable (x64)
Write-Host "
[1/4] Checking Microsoft Visual C++ Redistributable (x64)..." -ForegroundColor Cyan
 = False
try {
     = Get-ItemProperty -Path "HKLM:\SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x64" -ErrorAction SilentlyContinue
    if ( -and .Installed -eq 1) {  = True }
} catch {}

if () {
    Write-Host "  [OK] Visual C++ 2015-2022 Redistributable is already installed." -ForegroundColor Green
} else {
    Write-Host "  [+] Downloading VC++ 2015-2022 Redistributable..." -ForegroundColor Yellow
     = "https://aka.ms/vs/17/release/vc_redist.x64.exe"
     = Join-Path C:\Users\Kyle\AppData\Local\Temp "vc_redist.x64.exe"
    try {
        Invoke-WebRequest -Uri  -OutFile  -UseBasicParsing
        Write-Host "  [+] Installing VC++ Redistributable silently..." -ForegroundColor Yellow
        Start-Process -FilePath  -ArgumentList "/install /quiet /norestart" -Wait
        Remove-Item  -Force -ErrorAction SilentlyContinue
        Write-Host "  [OK] VC++ Redistributable installed successfully!" -ForegroundColor Green
    } catch {
        Write-Host "  [!] Warning installing VC++: " -ForegroundColor Red
    }
}

# 2. Check and Install Microsoft Edge WebView2 Runtime
Write-Host "
[2/4] Checking Microsoft Edge WebView2 Evergreen Runtime..." -ForegroundColor Cyan
 = False
try {
     = Get-ItemProperty -Path "HKLM:\SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-6F3A27050742}" -ErrorAction SilentlyContinue
    if (-not ) {
         = Get-ItemProperty -Path "HKCU:\Software\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-6F3A27050742}" -ErrorAction SilentlyContinue
    }
    if ( -and .pv -and .pv -ne "0.0.0.0") {  = True }
} catch {}

if () {
    Write-Host "  [OK] Microsoft Edge WebView2 Runtime is already installed." -ForegroundColor Green
} else {
    Write-Host "  [+] Downloading Microsoft Edge WebView2 Bootstrapper..." -ForegroundColor Yellow
     = "https://go.microsoft.com/fwlink/p/?LinkId=2124703"
     = Join-Path C:\Users\Kyle\AppData\Local\Temp "MicrosoftEdgeWebview2Setup.exe"
    try {
        Invoke-WebRequest -Uri  -OutFile  -UseBasicParsing
        Write-Host "  [+] Installing WebView2 Runtime silently..." -ForegroundColor Yellow
        Start-Process -FilePath  -ArgumentList "/silent /install" -Wait
        Remove-Item  -Force -ErrorAction SilentlyContinue
        Write-Host "  [OK] WebView2 Runtime installed successfully!" -ForegroundColor Green
    } catch {
        Write-Host "  [!] Warning installing WebView2: " -ForegroundColor Red
    }
}

# 3. Check and Install .NET 10 SDK & Desktop Runtime
Write-Host "
[3/4] Checking .NET 10 Desktop Runtime & SDK..." -ForegroundColor Cyan
 = Get-Command "dotnet" -ErrorAction SilentlyContinue
 = False

if () {
    try {
         = & dotnet --list-runtimes
        if ( -like "*Microsoft.WindowsDesktop.App 10.*" -or  -like "*Microsoft.NETCore.App 10.*") {
             = True
        }
    } catch {}
}

if () {
    Write-Host "  [OK] .NET 10 is already installed and available in PATH." -ForegroundColor Green
} else {
    Write-Host "  [+] .NET 10 is missing or outdated. Bootstrapping via official Microsoft CDN..." -ForegroundColor Yellow
     = Join-Path C:\Users\Kyle\AppData\Local\Temp "dotnet-install.ps1"
     = "C:\Program Files\dotnet"
    
    try {
        Write-Host "  [+] Downloading official dotnet-install.ps1..." -ForegroundColor Yellow
        Invoke-WebRequest -Uri "https://dot.net/v1/dotnet-install.ps1" -OutFile  -UseBasicParsing
        
        Write-Host "  [+] Installing .NET 10 Desktop Runtime (windowsdesktop)..." -ForegroundColor Yellow
        &  -Channel 10.0 -Runtime windowsdesktop -InstallDir 
        
        Write-Host "  [+] Installing .NET 10 SDK..." -ForegroundColor Yellow
        &  -Channel 10.0 -InstallDir 
        
        # Ensure PATH is updated
         = [Environment]::GetEnvironmentVariable('Path', 'Machine')
        if ( -notlike "**") {
            [Environment]::SetEnvironmentVariable('Path', ";", 'Machine')
        }
        C:\Program Files\WindowsApps\Microsoft.PowerShell_7.6.5.0_x64__8wekyb3d8bbwe;C:/Users/Kyle/.gemini/antigravity/bin;C:\Users\Kyle\AppData\Roaming\Antigravity\bin;C:\Python314\Scripts\;C:\Python314\;C:\WINDOWS\system32;C:\WINDOWS;C:\WINDOWS\System32\Wbem;C:\WINDOWS\System32\WindowsPowerShell\v1.0\;C:\WINDOWS\System32\OpenSSH\;C:\Program Files\Git\cmd;C:\ProgramData\chocolatey\bin;C:\Program Files\CMake\bin;C:\Program Files\Microsoft SQL Server\170\Tools\Binn\;C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\170\Tools\Binn\;C:\Program Files (x86)\Windows Kits\10\Windows Performance Toolkit\;C:\Program Files\Go\bin;C:\Program Files (x86)\Google\Cloud SDK\google-cloud-sdk\bin;C:\Program Files\GitHub CLI\;C:\Program Files\nodejs\;C:\Program Files\Git-Xet\;C:\Program Files\dotnet\;C:\Program Files\Docker\Docker\resources\bin;C:\Program Files\Cloudflare\Cloudflare WARP\;C:\Users\Kyle\.cargo\bin;\\?\C:\Users\Kyle\AppData\Local\Programs\Jan\resources\bin;C:\Users\Kyle\.kimi-code\bin;C:\Users\Kyle\.local\bin;C:\Users\Kyle\AppData\Local\Programs\Python\Python313\Scripts\;C:\Users\Kyle\AppData\Local\Programs\Python\Python313\;C:\Users\Kyle\AppData\Local\Microsoft\WindowsApps;C:\Users\Kyle\AppData\Local\Programs\Microsoft VS Code\bin;C:\Users\Kyle\.dotnet\tools;C:\Program Files\NASM;C:\msys64\ucrt64\bin;C:\Users\Kyle\go\bin;C:\Users\Kyle\AppData\Local\Programs\Antigravity IDE\bin;C:\Program Files\Obsidian;C:\Users\Kyle\AppData\Local\Microsoft\WinGet\Packages\Gyan.FFmpeg_Microsoft.Winget.Source_8wekyb3d8bbwe\ffmpeg-8.1.2-full_build\bin;C:\Users\Kyle\AppData\Roaming\npm;C:\Users\Kyle\flutter\bin;C:\Program Files\JetBrains\IntelliJ IDEA 2026.2.1\bin;C:\Users\Kyle\ngrok;C:\Users\Kyle\AppData\Local\Programs\Ollama;C:\Users\Kyle\.lmstudio\bin;C:\Program Files\qemu;C:\Users\Kyle\.dotnet\tools;C:\Users\Kyle\AppData\Local\lemonade_server\bin\ = "C:\Program Files\WindowsApps\Microsoft.PowerShell_7.6.5.0_x64__8wekyb3d8bbwe;C:/Users/Kyle/.gemini/antigravity/bin;C:\Users\Kyle\AppData\Roaming\Antigravity\bin;C:\Python314\Scripts\;C:\Python314\;C:\WINDOWS\system32;C:\WINDOWS;C:\WINDOWS\System32\Wbem;C:\WINDOWS\System32\WindowsPowerShell\v1.0\;C:\WINDOWS\System32\OpenSSH\;C:\Program Files\Git\cmd;C:\ProgramData\chocolatey\bin;C:\Program Files\CMake\bin;C:\Program Files\Microsoft SQL Server\170\Tools\Binn\;C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\170\Tools\Binn\;C:\Program Files (x86)\Windows Kits\10\Windows Performance Toolkit\;C:\Program Files\Go\bin;C:\Program Files (x86)\Google\Cloud SDK\google-cloud-sdk\bin;C:\Program Files\GitHub CLI\;C:\Program Files\nodejs\;C:\Program Files\Git-Xet\;C:\Program Files\dotnet\;C:\Program Files\Docker\Docker\resources\bin;C:\Program Files\Cloudflare\Cloudflare WARP\;C:\Users\Kyle\.cargo\bin;\\?\C:\Users\Kyle\AppData\Local\Programs\Jan\resources\bin;C:\Users\Kyle\.kimi-code\bin;C:\Users\Kyle\.local\bin;C:\Users\Kyle\AppData\Local\Programs\Python\Python313\Scripts\;C:\Users\Kyle\AppData\Local\Programs\Python\Python313\;C:\Users\Kyle\AppData\Local\Microsoft\WindowsApps;C:\Users\Kyle\AppData\Local\Programs\Microsoft VS Code\bin;C:\Users\Kyle\.dotnet\tools;C:\Program Files\NASM;C:\msys64\ucrt64\bin;C:\Users\Kyle\go\bin;C:\Users\Kyle\AppData\Local\Programs\Antigravity IDE\bin;C:\Program Files\Obsidian;C:\Users\Kyle\AppData\Local\Microsoft\WinGet\Packages\Gyan.FFmpeg_Microsoft.Winget.Source_8wekyb3d8bbwe\ffmpeg-8.1.2-full_build\bin;C:\Users\Kyle\AppData\Roaming\npm;C:\Users\Kyle\flutter\bin;C:\Program Files\JetBrains\IntelliJ IDEA 2026.2.1\bin;C:\Users\Kyle\ngrok;C:\Users\Kyle\AppData\Local\Programs\Ollama;C:\Users\Kyle\.lmstudio\bin;C:\Program Files\qemu;C:\Users\Kyle\.dotnet\tools;C:\Users\Kyle\AppData\Local\lemonade_server\bin\;"
        Remove-Item  -Force -ErrorAction SilentlyContinue
        Write-Host "  [OK] .NET 10 Desktop Runtime and SDK installed successfully!" -ForegroundColor Green
    } catch {
        Write-Host "  [!] Error installing .NET 10: " -ForegroundColor Red
    }
}

# 4. Check Windows Package Manager (winget)
Write-Host "
[4/4] Checking Windows Package Manager (winget)..." -ForegroundColor Cyan
 = Get-Command "winget" -ErrorAction SilentlyContinue
if () {
    Write-Host "  [OK] winget is available." -ForegroundColor Green
} else {
    Write-Host "  [+] Bootstrapping Windows Package Manager (winget)..." -ForegroundColor Yellow
    try {
         = Join-Path C:\Users\Kyle\AppData\Local\Temp ("winget_setup_" + [Guid]::NewGuid().ToString("N"))
        New-Item -ItemType Directory -Path  -Force | Out-Null
        
         = 'https://aka.ms/Microsoft.VCLibs.x64.14.00.Desktop.appx'
         = 'https://github.com/microsoft/microsoft-ui-xaml/releases/download/v2.8.6/Microsoft.UI.Xaml.2.8.x64.appx'
         = 'https://github.com/microsoft/winget-cli/releases/latest/download/Microsoft.DesktopAppInstaller_8wekyb3d8bbwe.msixbundle'

        Invoke-WebRequest -Uri  -OutFile "/vclibs.appx" -UseBasicParsing
        Invoke-WebRequest -Uri  -OutFile "/xaml.appx" -UseBasicParsing
        Invoke-WebRequest -Uri  -OutFile "/winget.msixbundle" -UseBasicParsing

        Add-AppxPackage -Path "/vclibs.appx" -ErrorAction SilentlyContinue
        Add-AppxPackage -Path "/xaml.appx" -ErrorAction SilentlyContinue
        Add-AppxPackage -Path "/winget.msixbundle" -DependencyPath @("/vclibs.appx", "/xaml.appx") -ErrorAction SilentlyContinue
        
        Remove-Item  -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host "  [OK] winget bootstrapped successfully!" -ForegroundColor Green
    } catch {
        Write-Host "  [!] Warning bootstrapping winget: " -ForegroundColor Red
    }
}

Write-Host "
================================================================" -ForegroundColor Cyan
Write-Host "  All system prerequisites and online packages are ready!" -ForegroundColor Green
Write-Host "================================================================" -ForegroundColor Cyan

# Check if csproj exists in current or parent directory to build and run
 = Join-Path  "HeaplitLauncher.csproj"
if (Test-Path ) {
    Write-Host "
[+] Building Heaplit..." -ForegroundColor Yellow
    Set-Location 
    & dotnet build HeaplitLauncher.csproj -c Debug
    if ( -eq 0) {
        Write-Host "
[+] Launching Heaplit..." -ForegroundColor Green
         = Join-Path  "bin\Debug\net10.0-windows\HeaplitLauncher.exe"
        if (Test-Path ) {
            Start-Process 
        } else {
            & dotnet run --project HeaplitLauncher.csproj
        }
    }
}
