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
echo   NACELA B2 - SOLO CUERPO VERDE SIMPLE
echo ============================================================
echo Se genera solamente:
echo   - cuerpo central mediante un loft de elipses sin guias
echo   - circulo frontal unido al cuerpo con un loft independiente
echo   - toma inferior rectangular redondeada con otro loft
echo   - uniones solidas mediante Combine/Add
echo No se generan pieza azul posterior, saddle, shell, huecos, conductos,
echo escapes, tomas laterales ni paneles.
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
