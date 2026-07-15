@echo off
setlocal EnableExtensions
cd /d "%~dp0"

if exist "00_PREPARAR_ALA.bat" call "00_PREPARAR_ALA.bat" /quiet
call BUILD.bat
if errorlevel 1 (
  pause
  exit /b 1
)

echo.
echo Generando solamente B1 Stage 1: OML nueva, gearbox y saddle...
"%~dp0bin\NacelleBuilder.exe" stage1
set "ERR=%ERRORLEVEL%"
if not "%ERR%"=="0" (
  echo Fallo B1 Stage 1. Revise ultimo_ejecucion.log.
) else (
  echo B1 Stage 1 creado en generated\B1.
)
pause
exit /b %ERR%
