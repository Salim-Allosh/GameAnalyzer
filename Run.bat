@echo off
setlocal enabledelayedexpansion

set LOG_FILE=run_log.txt
echo ========================================= > %LOG_FILE%
echo Starting at %DATE% %TIME% >> %LOG_FILE%
echo ========================================= >> %LOG_FILE%

echo [1/5] Stopping previous processes...
taskkill /F /IM SportsAnalytics.Desktop.exe /T >nul 2>&1
taskkill /F /IM VBCSCompiler.exe /T >nul 2>&1
for /f "tokens=5" %%a in ('netstat -aon ^| find ":5000" ^| find "LISTENING"') do taskkill /f /pid %%a >nul 2>&1

echo [2/5] Checking .NET SDK...
dotnet --version >> %LOG_FILE% 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo Error: .NET SDK not found.
    pause
    exit /b 1
)

echo [3/5] Restoring packages...
dotnet restore >> %LOG_FILE% 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo Error: Restore failed. Check %LOG_FILE%.
    pause
    exit /b 1
)

echo [4/5] Building project...
dotnet build --no-restore >> %LOG_FILE% 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo Error: Build failed. Check %LOG_FILE%.
    pause
    exit /b 1
)

echo [5/5] Launching Desktop Application...
start "Sports Analytics Engine" cmd /c "dotnet run --project SportsAnalytics.Desktop --no-build >> %LOG_FILE% 2>&1"

echo =========================================
echo Project started successfully!
echo Log saved to %LOG_FILE%
echo =========================================
pause
