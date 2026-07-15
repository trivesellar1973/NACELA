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
echo Generando B2 Stage 1: OML, boss circular, toma inferior solida y saddle...
"%~dp0bin\NacelleBuilder.exe" stage1
set "ERR=%ERRORLEVEL%"
if not "%ERR%"=="0" (
  echo Fallo B2 Stage 1. Revise ultimo_ejecucion.log.
) else (
  echo B2 Stage 1 creado en generated\B2.
)
pause
exit /b %ERR%
