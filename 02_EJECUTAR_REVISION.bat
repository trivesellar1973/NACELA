@echo off
setlocal EnableExtensions
cd /d "%~dp0"

rem Prepara el ala si existe un ZIP local. Si no aparece, el generador igual
rem crea la nacela desde cero y solamente omite el ensamblaje de revision.
if exist "00_PREPARAR_ALA.bat" call "00_PREPARAR_ALA.bat" /quiet

call BUILD.bat
if errorlevel 1 (
  pause
  exit /b 1
)

echo.
echo Ejecutando generador nativo de SOLIDWORKS - STAGE 1...
rem No pasar %%~dp0 como argumento: termina en barra invertida y podia
rem producir una comilla residual en Path.GetFullPath.
"%~dp0bin\NacelleBuilder.exe" stage1
set "ERR=%ERRORLEVEL%"
echo.
if not "%ERR%"=="0" (
  echo La ejecucion fallo. Revise ultimo_ejecucion.log.
) else (
  echo Ejecucion correcta.
  echo La nacela fue creada desde cero en generated\REVISION.
  echo Si el ala estaba disponible, tambien se creo el ensamblaje de revision.
  echo Envie capturas lateral, frontal e isometrica antes de agregar toma y escapes.
)
pause
exit /b %ERR%
