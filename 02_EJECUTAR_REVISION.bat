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
echo ============================================================
echo   NACELA B1 - RECONSTRUCCION COMPLETA DESDE CERO
echo ============================================================
echo Stage 1: OML nueva, gearbox y saddle de union al ala.
echo Stage 2: scoop rectangular ovalado, tomas laterales y escapes altos.
echo Stage 3: capos grandes y paneles funcionales.
echo.
"%~dp0bin\NacelleBuilder.exe" review
set "ERR=%ERRORLEVEL%"

echo.
if not "%ERR%"=="0" (
  echo La ejecucion fallo. Revise ultimo_ejecucion.log.
) else (
  echo Ejecucion correcta.
  echo Resultados en generated\B1.
  echo Abra ALA_REVIEW_NACELA_DER_B1.SLDASM
  echo o use 03_ABRIR_ULTIMO_RESULTADO.bat.
)
pause
exit /b %ERR%
