@echo off
setlocal
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0compile-host.ps1"
if errorlevel 1 (
  echo.
  echo Falha ao compilar SEZPrintServiceHost.exe.
  pause
  exit /b 1
)
echo.
echo SEZPrintServiceHost.exe compilado com sucesso na pasta dist.
pause
