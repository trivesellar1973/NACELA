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
echo   NACELA B2 - FRENTE SOLIDO NUEVO DESDE CERO
echo ============================================================
echo Se genera solamente:
echo   - OML principal nueva
echo   - boss circular del spinner integrado por loft
echo   - toma inferior rectangular ovalada completamente cerrada
echo   - saddle superior suave
echo No se generan shell, huecos, conductos, escapes ni paneles.
echo.
"%~dp0bin\NacelleBuilder.exe" review
set "ERR=%ERRORLEVEL%"

echo.
if not "%ERR%"=="0" (
  echo La ejecucion fallo. Revise ultimo_ejecucion.log.
) else (
  echo Ejecucion correcta.
  echo Resultados en generated\B2.
  echo Abra ALA_REVIEW_NACELA_DER_B2.SLDASM
  echo o use 03_ABRIR_ULTIMO_RESULTADO.bat.
)
pause
exit /b %ERR%
