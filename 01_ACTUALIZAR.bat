@echo off
setlocal EnableExtensions
cd /d "%~dp0"

set "REPO=trivesellar1973/NACELA"
set "BRANCH=main"
set "ZIP_URL=https://github.com/%REPO%/archive/refs/heads/%BRANCH%.zip"
set "SELF_NAME=01_ACTUALIZAR.bat"
set "SELF_NEW=%CD%\01_ACTUALIZAR.bat.new"

echo ============================================================
echo   ACTUALIZADOR NACELA
echo ============================================================
echo Carpeta local: %CD%
echo.

rem ------------------------------------------------------------
rem Caso 1: copia clonada con Git.
rem ------------------------------------------------------------
if exist ".git\" (
    where git >nul 2>&1
    if errorlevel 1 (
        echo ERROR: esta carpeta tiene .git, pero Git no esta instalado
        echo o no esta agregado al PATH.
        pause
        exit /b 1
    )

    echo Se detecto un clon Git. Descargando cambios...
    git pull --ff-only origin %BRANCH%
    if errorlevel 1 (
        echo.
        echo ERROR: Git no pudo actualizar.
        echo Puede haber cambios locales, falta de Internet o una rama distinta.
        pause
        exit /b 1
    )

    call :CLEAN_LEGACY
    echo.
    echo Repositorio actualizado correctamente con Git.
    pause
    exit /b 0
)

rem ------------------------------------------------------------
rem Caso 2: carpeta descargada como ZIP.
rem IMPORTANTE: el BAT activo se excluye de Robocopy. Sobrescribir el
rem propio archivo mientras CMD lo ejecuta corrompe la lectura y genera
rem errores como "ODE no se reconoce como comando".
rem ------------------------------------------------------------
echo No se encontro la carpeta .git.
echo Esta copia fue descargada como ZIP; se usara actualizacion directa.
echo.

set "TMP_ROOT=%TEMP%\NACELA_UPDATE_%RANDOM%_%RANDOM%"
set "TMP_ZIP=%TMP_ROOT%\NACELA-main.zip"
set "TMP_EXTRACT=%TMP_ROOT%\extract"
set "SOURCE=%TMP_EXTRACT%\NACELA-%BRANCH%"

mkdir "%TMP_ROOT%" >nul 2>&1
mkdir "%TMP_EXTRACT%" >nul 2>&1

echo Descargando la ultima version desde GitHub...
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$ErrorActionPreference='Stop'; Invoke-WebRequest -UseBasicParsing -Uri '%ZIP_URL%' -OutFile '%TMP_ZIP%'; Expand-Archive -LiteralPath '%TMP_ZIP%' -DestinationPath '%TMP_EXTRACT%' -Force"

if errorlevel 1 goto DOWNLOAD_ERROR

if not exist "%SOURCE%\README.md" goto STRUCTURE_ERROR
if not exist "%SOURCE%\%SELF_NAME%" goto STRUCTURE_ERROR

echo Copiando archivos actualizados...
robocopy "%SOURCE%" "%CD%" /E /COPY:DAT /DCOPY:DAT /R:2 /W:1 /NFL /NDL /NJH /NJS /NP ^
  /XD ".git" "generated" ".vs" "bin" "obj" ^
  /XF "local.ini" "ultimo_ejecucion.log" "%SELF_NAME%"

set "ROBOCOPY_CODE=%ERRORLEVEL%"
if %ROBOCOPY_CODE% GEQ 8 goto ROBOCOPY_ERROR

rem Guardar la version nueva del actualizador con otro nombre. Se reemplaza
rem despues de cerrar este proceso mediante un helper temporal.
copy /y "%SOURCE%\%SELF_NAME%" "%SELF_NEW%" >nul
if errorlevel 1 goto SELF_COPY_ERROR

call :CLEAN_LEGACY
rmdir /s /q "%TMP_ROOT%" >nul 2>&1

echo.
echo Proyecto actualizado correctamente desde el ZIP de GitHub.
echo Se conservaron generated\, config\local.ini y ultimo_ejecucion.log.
echo Se eliminaron fuentes antiguas que podian duplicar clases B1.
echo El actualizador se reemplazara al cerrar esta ventana.
pause

call :SCHEDULE_SELF_REPLACE
exit /b 0

:DOWNLOAD_ERROR
echo.
echo ERROR: no se pudo descargar o descomprimir la actualizacion.
rmdir /s /q "%TMP_ROOT%" >nul 2>&1
pause
exit /b 1

:STRUCTURE_ERROR
echo.
echo ERROR: la descarga no contiene la estructura esperada.
rmdir /s /q "%TMP_ROOT%" >nul 2>&1
pause
exit /b 1

:ROBOCOPY_ERROR
echo.
echo ERROR: Robocopy fallo con codigo %ROBOCOPY_CODE%.
rmdir /s /q "%TMP_ROOT%" >nul 2>&1
pause
exit /b 1

:SELF_COPY_ERROR
echo.
echo ERROR: se copiaron los archivos del proyecto, pero no se pudo preparar
echo la nueva version de %SELF_NAME%.
rmdir /s /q "%TMP_ROOT%" >nul 2>&1
pause
exit /b 1

:CLEAN_LEGACY
rem Las actualizaciones ZIP no eliminan archivos que desaparecieron del repo.
rem Estos builders y tipos pertenecen a A0/A1/A2 y no deben compilarse en B1.
for %%F in (
  "src\NacelleConfig.cs"
  "src\LegacyGeometryTypes.cs"
  "src\NacelleStage1Builder.cs"
  "src\NacelleStage2Builder.cs"
  "src\NacelleStage3Builder.cs"
  "src\AssemblyReviewBuilder.cs"
) do (
  if exist "%%~F" del /q "%%~F" >nul 2>&1
)
exit /b 0

:SCHEDULE_SELF_REPLACE
set "SWAP_CMD=%TEMP%\NACELA_SWAP_%RANDOM%_%RANDOM%.cmd"
>"%SWAP_CMD%" echo @echo off
>>"%SWAP_CMD%" echo ping 127.0.0.1 -n 2 ^>nul
>>"%SWAP_CMD%" echo copy /y "%SELF_NEW%" "%CD%\%SELF_NAME%" ^>nul
>>"%SWAP_CMD%" echo del /q "%SELF_NEW%" ^>nul 2^>^&1
>>"%SWAP_CMD%" echo del /q "%%~f0" ^>nul 2^>^&1
start "" /b cmd /c "%SWAP_CMD%"
exit /b 0
