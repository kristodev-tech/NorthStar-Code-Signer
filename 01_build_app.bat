@echo off
call "%~dp0Build_App.bat"
set "RESULT=%ERRORLEVEL%"
pause
exit /b %RESULT%
