@echo off
setlocal enabledelayedexpansion

set LOG_FILE=save_log.txt
echo ========================================= > %LOG_FILE%
echo Starting save and sync at %DATE% %TIME% >> %LOG_FILE%
echo ========================================= >> %LOG_FILE%

echo [1/5] Checking Git repository...
git status >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo Repository not found locally, initializing Git...
    git init >> %LOG_FILE% 2>&1
)

echo [2/5] Setting up GitHub remote...
git remote -v | findstr "origin" >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    git remote add origin https://github.com/Salim-Allosh/GameAnalyzer.git >> %LOG_FILE% 2>&1
) else (
    git remote set-url origin https://github.com/Salim-Allosh/GameAnalyzer.git >> %LOG_FILE% 2>&1
)

echo [3/5] Adding files (git add)...
git add . >> %LOG_FILE% 2>&1

echo [4/5] Committing changes (git commit)...
set "COMMIT_MSG=Auto-update at %DATE% %TIME%"

git commit -m "%COMMIT_MSG%" >> %LOG_FILE% 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo Note: No new changes to commit, or commit failed. See log.
) else (
    echo Changes committed locally successfully!
)

echo [5/5] Pushing to GitHub (main branch)...
git branch -M main >> %LOG_FILE% 2>&1
git push -u origin main >> %LOG_FILE% 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo Error: Failed to push to GitHub.
    echo Please check your internet connection and credentials.
    pause
    exit /b 1
)

echo =========================================
echo All changes synced and pushed to GitHub successfully!
echo Project URL: https://github.com/Salim-Allosh/GameAnalyzer
echo =========================================

set TAG_NAME=Backup_%DATE:/=-%_%TIME::=-%
set TAG_NAME=%TAG_NAME: =_%
set TAG_NAME=%TAG_NAME:,=-%
set TAG_NAME=%TAG_NAME:.=-%
echo Creating local backup tag (Tag: %TAG_NAME%)...
git tag %TAG_NAME% >> %LOG_FILE% 2>&1

pause
