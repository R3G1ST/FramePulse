@echo off
setlocal
set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" set CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe

if not exist "dist" mkdir dist

"%CSC%" /nologo /target:winexe /optimize+ /platform:anycpu ^
  /win32icon:icon.ico ^
  /win32manifest:app.manifest ^
  /r:System.dll /r:System.Core.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:Microsoft.VisualBasic.dll ^
  /out:dist\FpsOverlay.exe ^
  Program.cs Native.cs Config.cs PresentMonClient.cs HardwareMonitor.cs OverlayForm.cs SettingsForm.cs

if %errorlevel%==0 (
  copy /y PresentMon.exe dist\PresentMon.exe >nul
  copy /y icon.ico dist\icon.ico >nul
  echo Build OK: dist\FpsOverlay.exe
) else (
  echo Build FAILED
  exit /b 1
)
endlocal
