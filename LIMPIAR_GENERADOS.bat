@echo off
cd /d "%~dp0"
if exist generated rmdir /s /q generated
if exist bin rmdir /s /q bin
if exist ultimo_ejecucion.log del /q ultimo_ejecucion.log
echo Generados eliminados.
pause
