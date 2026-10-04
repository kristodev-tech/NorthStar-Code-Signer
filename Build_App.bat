@echo off
setlocal
cd /d "%~dp0"
set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not exist "%CSC%" (
    echo Install Microsoft .NET Framework 4.8, then try again.
    exit /b 1
)
echo Building NorthStar Code Signer 2.0...
"%CSC%" /nologo /optimize+ /warn:4 /warnaserror+ /target:winexe /platform:anycpu /out:NorthStarCodeSigner.new.exe /win32icon:assets\northstar.ico /win32manifest:assets\app.manifest /resource:powershell\worker.ps1,worker.ps1 /resource:assets\northstar.ico,northstar.ico /resource:LICENSE,LICENSE /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Web.Extensions.dll src\*.cs
if errorlevel 1 exit /b 1
move /y NorthStarCodeSigner.new.exe NorthStarCodeSigner.exe >nul
if errorlevel 1 exit /b 1
echo Done: NorthStarCodeSigner.exe
exit /b 0
