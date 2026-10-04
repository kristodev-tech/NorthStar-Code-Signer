@echo off
setlocal
cd /d "%~dp0"
if not exist NorthStarCodeSigner.exe (
    echo Build the app first.
    pause
    exit /b 1
)
if not exist build mkdir build
start "" /wait NorthStarCodeSigner.exe --self-test "%CD%\build\Windows_Self_Test.json"
set "RESULT=%ERRORLEVEL%"
if exist build\Windows_Self_Test.json type build\Windows_Self_Test.json
if not "%RESULT%"=="0" goto finish
start "" /wait NorthStarCodeSigner.exe --ui-check-all "%CD%\build\ui"
set "RESULT=%ERRORLEVEL%"
echo Inspect the screenshots in build\ui and follow docs\RELEASE_CHECKLIST.md.
:finish
echo Result: %RESULT%
pause
exit /b %RESULT%
