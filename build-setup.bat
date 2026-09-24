@echo off
setlocal
set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" set CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe

if not exist "dist\FramePulse.exe" (
  echo Build FramePulse.exe first: build.bat
  exit /b 1
)
if not exist "dist\PresentMon.exe" copy /y PresentMon.exe dist\PresentMon.exe >nul
if not exist "dist\icon.ico" copy /y icon.ico dist\icon.ico >nul
if not exist "out" mkdir out

"%CSC%" /nologo /target:winexe /optimize+ /platform:anycpu ^
  /win32icon:icon.ico ^
  /win32manifest:setup.manifest ^
  /r:System.dll /r:System.Core.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:Microsoft.CSharp.dll ^
  /resource:dist\FramePulse.exe,fp.app.exe ^
  /resource:dist\PresentMon.exe,fp.pm.exe ^
  /resource:dist\icon.ico,fp.icon.ico ^
  /resource:config.default.ini,fp.config.ini ^
  /out:out\FramePulse-Setup.exe ^
  Installer.cs

if %errorlevel%==0 (
  if not exist "..\installer" mkdir "..\installer"
  copy /y out\FramePulse-Setup.exe "..\installer\FramePulse-Setup.exe" >nul
  echo Build OK: out\FramePulse-Setup.exe
  echo Copied:   ..\installer\FramePulse-Setup.exe
) else (
  echo Build FAILED
  exit /b 1
)
endlocal
