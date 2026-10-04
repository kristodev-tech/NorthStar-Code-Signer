@echo off
setlocal
cd /d "%~dp0"
if not exist NorthStarCodeSigner.exe (
    call Build_App.bat
    if errorlevel 1 goto failed
)
echo Packaging the existing EXE. Sign it before this step if desired.
set "ISCC=%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe"
if not exist "%ISCC%" set "ISCC=%ProgramFiles%\Inno Setup 6\ISCC.exe"
if not exist "%ISCC%" (
    echo Install Inno Setup 6 to build the optional installer.
    goto failed
)
"%ISCC%" installer\NorthStarCodeSigner.iss
if errorlevel 1 goto failed
echo Installer created in dist.
pause
exit /b 0
:failed
pause
exit /b 1
