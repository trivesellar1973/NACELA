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
echo Ejecutando B1 completo con Stage 3 de capos y paneles...
"%~dp0bin\NacelleBuilder.exe" stage3
set "ERR=%ERRORLEVEL%"
if not "%ERR%"=="0" (
  echo Fallo B1 Stage 3. Revise ultimo_ejecucion.log.
) else (
  echo B1 Stage 3 creado en generated\B1.
  echo Abra NACELA_DERECHA_B1_STAGE3_FINAL.SLDPRT.
)
pause
exit /b %ERR%
