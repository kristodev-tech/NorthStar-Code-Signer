@echo off
cd /d "%~dp0"
if not exist NorthStarCodeSigner.exe (
    call Build_App.bat
    if errorlevel 1 (
        pause
        exit /b 1
    )
)
start "" NorthStarCodeSigner.exe
