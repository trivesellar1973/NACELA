@echo off
setlocal
cd /d "%~dp0"
echo Ruta actual sugerida:
echo %USERPROFILE%\Desktop\AlaSW\ALA_COMPLETA_MECANISMOS.SLDASM
echo.
set /p "ASM=Pegue la ruta completa del ensamblaje base: "
if "%ASM%"=="" exit /b 1
>config\local.ini echo # Configuracion local - no se versiona
>>config\local.ini echo source_assembly=%ASM%
echo.
echo Guardado en config\local.ini
pause
