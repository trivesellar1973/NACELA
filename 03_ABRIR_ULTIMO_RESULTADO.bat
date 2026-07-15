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

set "ASM=%~dp0generated\%REV%\ALA_REVIEW_NACELA_DER_%REV%.SLDASM"
set "PART=%~dp0generated\%REV%\NACELA_DERECHA_%REV%_STAGE1_FRENTE_SOLIDO.SLDPRT"

if exist "%ASM%" (
  start "" "%ASM%"
  exit /b 0
)
if exist "%PART%" (
  echo No se genero ensamblaje porque no estaba disponible el ala base.
  echo Abriendo la nacela B2 frontal solida.
  start "" "%PART%"
  exit /b 0
)

echo No existe ningun resultado de la revision %REV%.
echo Ejecute primero 02_EJECUTAR_REVISION.bat
pause
exit /b 2
