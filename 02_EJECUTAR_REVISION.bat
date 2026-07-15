@echo off
setlocal EnableExtensions
cd /d "%~dp0"

rem Prepara el ala si existe un ZIP local. Si no aparece, el generador igual
rem crea las piezas de nacela y solamente omite el ensamblaje de revision.
if exist "00_PREPARAR_ALA.bat" call "00_PREPARAR_ALA.bat" /quiet

call BUILD.bat
if errorlevel 1 (
  pause
  exit /b 1
)

echo.
echo Ejecutando generador nativo de SOLIDWORKS - REVISION PROFESIONAL A2...
echo Stage 1: OML, gearbox, envolvente y saddle fairing.
echo Stage 2: toma chin, ducto, escapes enrasados y entradas NACA.
echo Stage 3: capos laterales, firewall y panel de servicio.
"%~dp0bin\NacelleBuilder.exe" review
set "ERR=%ERRORLEVEL%"

echo.
if not "%ERR%"=="0" (
  echo La ejecucion fallo. Revise ultimo_ejecucion.log.
) else (
  echo Ejecucion correcta.
  echo Resultados en generated\A2.
  echo Abra ALA_REVIEW_NACELA_DER_A2.SLDASM o use 03_ABRIR_ULTIMO_RESULTADO.bat.
  echo Envie capturas lateral, frontal, planta e isometrica.
)
pause
exit /b %ERR%
