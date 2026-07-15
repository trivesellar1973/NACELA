@echo off
setlocal EnableExtensions EnableDelayedExpansion
cd /d "%~dp0"
set "REV="
for /f "tokens=1,* delims==" %%A in (config\defaults.ini) do (
  if /I "%%A"=="revision" set "REV=%%B"
)
if not defined REV (
  echo No se pudo leer revision en config\defaults.ini
  pause
  exit /b 1
)
set "FILE=%~dp0generated\%REV%\ALA_REVIEW_NACELA_DER_%REV%.SLDASM"
if not exist "%FILE%" (
  echo No existe: %FILE%
  echo Ejecute primero 02_EJECUTAR_REVISION.bat
  pause
  exit /b 2
)
start "" "%FILE%"
