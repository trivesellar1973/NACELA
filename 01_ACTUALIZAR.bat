@echo off
setlocal
cd /d "%~dp0"
echo Actualizando NACELA desde GitHub...
git pull --ff-only
if errorlevel 1 (
  echo.
  echo No se pudo actualizar. Revise Git, Internet o cambios locales.
  pause
  exit /b 1
)
echo.
echo Repositorio actualizado.
pause
