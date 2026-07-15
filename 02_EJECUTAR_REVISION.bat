@echo off
setlocal
cd /d "%~dp0"
call BUILD.bat
if errorlevel 1 (
  pause
  exit /b 1
)

echo.
echo Ejecutando generador nativo de SOLIDWORKS - STAGE 1...
"%~dp0bin\NacelleBuilder.exe" stage1 "%~dp0"
set "ERR=%ERRORLEVEL%"
echo.
if not "%ERR%"=="0" (
  echo La ejecucion fallo. Revise ultimo_ejecucion.log.
) else (
  echo Ejecucion correcta. La pieza y el ensamblaje estan en generated\REVISION.
  echo Envie capturas lateral, frontal e isometrica antes de agregar toma y escapes.
)
pause
exit /b %ERR%
